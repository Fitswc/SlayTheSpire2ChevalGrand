"""Check a running mod through both stdio MCP and HTTP without changing the run."""

import asyncio
import json
import os
import sys
from pathlib import Path
from uuid import uuid4

import httpx
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


async def read_event(response: httpx.Response) -> dict[str, str]:
    response.raise_for_status()
    event: dict[str, str] = {}
    async for line in response.aiter_lines():
        if not line and event:
            return event
        if ": " in line:
            key, value = line.split(": ", 1)
            event[key] = value
    raise RuntimeError("SSE stream closed before an event arrived.")


async def main() -> None:
    port = os.environ.get("STS2_MCP_PORT", "15527")
    endpoint = f"http://127.0.0.1:{port}/events"
    parameters = StdioServerParameters(
        command=sys.executable,
        args=[str(Path(__file__).with_name("server.py").resolve())],
        env={"STS2_MCP_PORT": port},
    )

    async with httpx.AsyncClient(timeout=8, trust_env=False) as client:
        async with client.stream("GET", endpoint) as response:
            initial = await read_event(response)
            assert initial["event"] == "snapshot"

        async with stdio_client(parameters) as (read, write):
            async with ClientSession(read, write) as session:
                await session.initialize()
                tools = {tool.name for tool in (await session.list_tools()).tools}
                assert tools == {"health_check", "get_game_state", "get_available_actions", "act", "get_action_result"}

                async def call(tool: str, **kwargs):
                    result = await session.call_tool(tool, kwargs)
                    if result.isError or result.structuredContent is None:
                        raise RuntimeError(f"{tool} failed: {result.content}")
                    return result.structuredContent

                assert (await call("health_check"))["ready"]
                snapshot = await call("get_game_state")
                request_id = str(uuid4())
                args = {
                    "session_id": snapshot["sessionId"],
                    "revision": snapshot["revision"],
                    "action_id": "smoke-test:invalid",
                    "request_id": request_id,
                }
                result = await call("act", **args)
                assert result["status"] == "rejected" and result["error"] == "illegal_action"
                assert await call("act", **args) == result
                assert await call("get_action_result", request_id=request_id) == result
                stale = dict(args, revision=snapshot["revision"] + 1_000_000, request_id=str(uuid4()))
                assert (await call("act", **stale))["error"] == "stale_revision"

        async with client.stream("GET", endpoint, headers={"Last-Event-ID": initial["id"]}) as response:
            response.raise_for_status()
            replayed = False
            async with asyncio.timeout(8):
                async for line in response.aiter_lines():
                    if line.startswith("data: "):
                        event = json.loads(line[6:])
                        data = event.get("data", {})
                        if data.get("result", {}).get("requestId") == request_id:
                            assert data["source"] == "mcp"
                            assert data["result"] == result
                            replayed = True
                            break
            assert replayed

    print("MCP discovery, action validation, deduplication, result lookup, and SSE replay passed.")


if __name__ == "__main__":
    asyncio.run(main())
