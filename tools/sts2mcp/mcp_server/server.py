import asyncio
import os
from typing import Any
from uuid import UUID, uuid4

import httpx
from mcp.server.fastmcp import FastMCP


server = FastMCP(
    "STS2MCP",
    log_level="WARNING",
    instructions=(
        "Read the current state before acting. Choose an action ID listed in that revision "
        "and pass its session ID and revision to act. Read state again after each action. "
        "An empty action list can mean the game is transitioning or waiting for manual input."
    ),
)


def base_url() -> str:
    port = int(os.environ.get("STS2_MCP_PORT", "15527"))
    if not 1024 <= port <= 65535:
        raise ValueError("STS2_MCP_PORT must be between 1024 and 65535.")
    return f"http://127.0.0.1:{port}"


async def request(
    client: httpx.AsyncClient, method: str, path: str, body: dict[str, Any] | None = None
) -> dict[str, Any]:
    try:
        response = await client.request(method, base_url() + path, json=body)
    except httpx.RequestError as exc:
        message = f"Game bridge unavailable at {base_url()}: {type(exc).__name__}."
        if body is not None:
            message += f" Check request {body['requestId']} before retrying."
        raise RuntimeError(message) from exc
    result = response.json()
    if response.is_error and "status" not in result:
        raise RuntimeError(f"Game bridge returned {response.status_code}: {result.get('error')}.")
    return result


async def read(path: str) -> dict[str, Any]:
    async with httpx.AsyncClient(timeout=3, trust_env=False) as client:
        return await request(client, "GET", path)


@server.tool()
async def health_check() -> dict[str, Any]:
    """Check whether the local mod is running and has published its first state."""
    return await read("/health")


@server.tool()
async def get_game_state() -> dict[str, Any]:
    """Read the current screen, visible run and combat state, and available actions."""
    return await read("/state")


@server.tool()
async def get_available_actions() -> dict[str, Any]:
    """List legal actions with the session ID and revision required to execute one."""
    snapshot = await read("/state")
    return {
        "sessionId": snapshot["sessionId"],
        "revision": snapshot["revision"],
        "screen": snapshot["state"]["screen"],
        "actions": snapshot["state"]["actions"],
    }


@server.tool()
async def act(
    session_id: UUID, revision: int, action_id: str, request_id: UUID | None = None, seed: str | None = None
) -> dict[str, Any]:
    """Execute a listed action. Reuse request_id for retries and use seed only for embark.

    The result reports whether the game rejected the action or the observed state changed.
    Read the new state to verify the gameplay effect before issuing the next action.
    """
    identifier = str(request_id or uuid4())
    body = {
        "sessionId": str(session_id),
        "revision": revision,
        "actionId": action_id,
        "requestId": identifier,
        "seed": seed,
    }
    async with httpx.AsyncClient(timeout=3, trust_env=False) as client:
        result = await request(client, "POST", "/actions", body)
        deadline = asyncio.get_running_loop().time() + 6
        while result["status"] == "queued" and asyncio.get_running_loop().time() < deadline:
            await asyncio.sleep(0.15)
            result = await request(client, "GET", f"/actions/{identifier}")
        return result


@server.tool()
async def get_action_result(request_id: UUID) -> dict[str, Any]:
    """Look up the status of a previously submitted action in the current game session."""
    return await read(f"/actions/{request_id}")


if __name__ == "__main__":
    server.run(transport="stdio")
