# STS2MCP

在《杀戮尖塔 2》的 Windows Steam 单人模式中读取局面、订阅游戏操作，并通过 MCP 工具执行测试动作。

## 构建与安装

Mod 以 Windows Steam 游戏 `v0.113.0` 的 .NET 9 程序集为构建目标。可将安装包内的两个文件解压到游戏目录下的 `mods/STS2MCP/`。如需从源码构建，请安装 .NET SDK 并从项目根目录运行以下命令。

```powershell
$game = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'
dotnet build mod/STS2MCP.csproj -c Release "-p:Sts2Dir=$game" -o dist/STS2MCP
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed' }
$mod = Join-Path $game 'mods/STS2MCP'
New-Item -ItemType Directory -Force -Path $mod | Out-Null
Copy-Item -LiteralPath 'dist/STS2MCP/STS2MCP.dll','dist/STS2MCP/STS2MCP.json' -Destination $mod
```

关闭游戏后复制文件。首次安装请在游戏中同意加载 Mod，并重启游戏。建议使用单独的游戏测试档。

## 连接 Codex

安装 Python 3.11+ 与 `uv` 后，从项目根目录运行以下命令，再启动新的 Codex 会话。

```powershell
uv sync --project mcp_server --locked --no-install-project
codex mcp add sts2 -- uv --directory (Join-Path $PWD 'mcp_server') run --locked --no-install-project python server.py
```

默认地址为 `127.0.0.1:15527`。如果更改端口，游戏进程和 MCP 服务必须使用相同的 `STS2_MCP_PORT`。本机进程可以访问该端口和操作当前单人局。

1. 用 `health_check` 确认 Mod 在线。用 `get_game_state` 读取局面，或用 `get_available_actions` 获取精简动作列表。
2. 把当前快照的 `sessionId`、`revision` 和选中的 `id` 传给 `act`。调用 `embark` 时可以提供固定 `seed`。
3. 每次操作后重新读取局面。状态为 `state_changed` 时核对具体效果。收到 `queued` 或遇到连接超时时，可用原 `request_id` 查询或重试同一请求。

## HTTP 接口

| 接口 | 内容 |
|---|---|
| `GET /health` | 协议版本、游戏会话 ID 和快照就绪状态 |
| `GET /state` | 当前快照、状态版本和可用动作 |
| `GET /events` | SSE 快照、状态变化和操作事件 |
| `POST /actions` | 接收 JSON 字段 `sessionId`、`revision`、`actionId`、`requestId` 及可选的 `seed` |
| `GET /actions/{requestId}` | 查询请求状态 |

```powershell
curl.exe -N http://127.0.0.1:15527/events
```

事件 ID 由游戏会话 ID 和递增序号组成。重连时发送 `Last-Event-ID` 可补收最近的事件。缓存已过期或游戏已重启时会收到当前快照。操作事件中的 `source` 为 `manual` 或 `mcp`。手动事件的 `dispatched` 表示游戏接收了受监控的操作，后续 `state` 事件提供实际局面。MCP 请求在游戏线程重新校验，重复的请求 ID 在同一游戏会话中返回原结果。

游戏转场、动画或未覆盖的界面可能暂时没有可用动作。等待下一次快照，或在游戏中处理当前弹窗后继续。

游戏稳定停在一个界面时，可从项目根目录运行协议冒烟测试。测试会提交一个无效动作，核对拒绝、去重、结果查询和 SSE 重放。

```powershell
uv --directory mcp_server run --locked --no-install-project python smoke_test.py
```
