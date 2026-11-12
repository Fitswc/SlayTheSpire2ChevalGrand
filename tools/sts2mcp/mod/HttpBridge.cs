using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace STS2MCP;

internal sealed class HttpBridge : IDisposable
{
    private const int MaxHeaderBytes = 8192;
    private const int MaxBodyBytes = 4096;
    private static readonly byte[] HeaderEnd = "\r\n\r\n"u8.ToArray();
    private readonly BridgeState state;
    private readonly TcpListener listener;
    private readonly CancellationTokenSource stopping = new();
    private readonly SemaphoreSlim connections = new(32);
    private readonly SemaphoreSlim streams = new(8);
    private readonly string authority;

    private sealed record Request(string Method, string Path, Dictionary<string, string> Headers, byte[] Body);

    public HttpBridge(BridgeState state, int port)
    {
        this.state = state;
        authority = $"127.0.0.1:{port}";
        listener = new TcpListener(IPAddress.Loopback, port);
        listener.ExclusiveAddressUse = true;
        listener.Start(32);
        _ = AcceptAsync(stopping.Token);
    }

    private async Task AcceptAsync(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(cancellation).ConfigureAwait(false); }
            catch (Exception e) when (e is SocketException or ObjectDisposedException or OperationCanceledException)
            {
                return;
            }
            if (connections.Wait(0))
                _ = HandleAsync(client, cancellation);
            else
                client.Dispose();
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellation)
    {
        using (client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                Request? request;
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
                {
                    deadline.CancelAfter(TimeSpan.FromSeconds(5));
                    request = await ReadRequestAsync(stream, deadline.Token);
                }
                if (request is null)
                    await ReplyAsync(stream, 400, new { error = "invalid_http_request" }, cancellation);
                else if (request.Headers.GetValueOrDefault("Host") != authority || request.Headers.ContainsKey("Origin"))
                    await ReplyAsync(stream, 403, new { error = "loopback_clients_only" }, cancellation);
                else
                    await RouteAsync(stream, request, cancellation);
            }
            catch (Exception e) when (e is IOException or SocketException or OperationCanceledException or ObjectDisposedException)
            {
                // Clients can disconnect during any response, including an event stream.
            }
            finally
            {
                connections.Release();
            }
        }
    }

    private static async Task<Request?> ReadRequestAsync(NetworkStream stream, CancellationToken cancellation)
    {
        // Each connection carries exactly one request with a bounded header and a fixed-length body.
        byte[] buffer = new byte[MaxHeaderBytes];
        int length = 0;
        int headerEnd = -1;
        while (length < buffer.Length && headerEnd == -1)
        {
            int start = Math.Max(0, length - HeaderEnd.Length + 1);
            int count = await stream.ReadAsync(buffer.AsMemory(length), cancellation);
            if (count == 0)
                return null;
            length += count;
            int marker = buffer.AsSpan(start, length - start).IndexOf(HeaderEnd);
            if (marker >= 0)
                headerEnd = start + marker;
        }
        if (headerEnd < 0)
            return null;

        string[] lines = Encoding.ASCII.GetString(buffer, 0, headerEnd).Split("\r\n");
        string[] requestLine = lines[0].Split(' ');
        if (requestLine.Length != 3 || requestLine[2] is not ("HTTP/1.0" or "HTTP/1.1"))
            return null;
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines.Skip(1))
        {
            int colon = line.IndexOf(':');
            if (colon < 1 || !headers.TryAdd(line[..colon], line[(colon + 1)..].Trim()))
                return null;
        }
        if (headers.ContainsKey("Transfer-Encoding") || headers.ContainsKey("Expect"))
            return null;
        if (!int.TryParse(headers.GetValueOrDefault("Content-Length", "0"), NumberStyles.None, CultureInfo.InvariantCulture,
                out int bodyLength) || bodyLength > MaxBodyBytes)
            return null;
        byte[] body = new byte[bodyLength];
        int bodyStart = headerEnd + HeaderEnd.Length;
        int buffered = length - bodyStart;
        if (buffered > bodyLength)
            return null;
        buffer.AsSpan(bodyStart, buffered).CopyTo(body);
        if (buffered < bodyLength)
            await stream.ReadExactlyAsync(body.AsMemory(buffered), cancellation);
        return new Request(requestLine[0], requestLine[1], headers, body);
    }

    private async Task RouteAsync(NetworkStream stream, Request request, CancellationToken cancellation)
    {
        switch (request.Method, request.Path)
        {
            case ("GET", "/health"):
                await ReplyAsync(stream, 200, new { protocol = 1, sessionId = state.SessionId, ready = state.Snapshot is not null }, cancellation);
                break;
            case ("GET", "/state"):
                if (state.Snapshot is { } snapshot)
                    await ReplyAsync(stream, 200, snapshot, cancellation);
                else
                    await ReplyAsync(stream, 503, new { error = "game_not_ready" }, cancellation);
                break;
            case ("GET", "/events"):
                if (!streams.Wait(0))
                {
                    await ReplyAsync(stream, 503, new { error = "event_stream_limit" }, cancellation);
                    break;
                }
                try { await StreamAsync(stream, request.Headers.GetValueOrDefault("Last-Event-ID"), cancellation); }
                finally { streams.Release(); }
                break;
            case ("POST", "/actions"):
                await SubmitAsync(stream, request, cancellation);
                break;
            case ("GET", _) when request.Path.StartsWith("/actions/", StringComparison.Ordinal)
                && Guid.TryParse(request.Path.AsSpan("/actions/".Length), out Guid requestId):
                if (state.GetResult(requestId) is { } result)
                    await ReplyAsync(stream, 200, result, cancellation);
                else
                    await ReplyAsync(stream, 404, new { error = "unknown_request" }, cancellation);
                break;
            default:
                await ReplyAsync(stream, 404, new { error = "unknown_route" }, cancellation);
                break;
        }
    }

    private async Task SubmitAsync(NetworkStream stream, Request request, CancellationToken cancellation)
    {
        string? type = request.Headers.GetValueOrDefault("Content-Type")?.Split(';')[0].Trim();
        if (!string.Equals(type, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            await ReplyAsync(stream, 415, new { error = "application_json_required" }, cancellation);
            return;
        }
        ActionRequest? action;
        try { action = JsonSerializer.Deserialize<ActionRequest>(request.Body, BridgeState.Json); }
        catch (JsonException) { action = null; }
        if (action is null || action.RequestId == Guid.Empty || action.SessionId == Guid.Empty
            || action.Revision <= 0 || string.IsNullOrWhiteSpace(action.ActionId) || action.ActionId.Length > 256
            || action.Seed is { } seed && (seed.Length is < 1 or > 12 || seed.Any(c => !char.IsAsciiLetterOrDigit(c))))
        {
            await ReplyAsync(stream, 400, new { error = "invalid_action_request" }, cancellation);
            return;
        }
        var (code, result) = state.Submit(action);
        await ReplyAsync(stream, code, result, cancellation);
    }

    private async Task StreamAsync(NetworkStream stream, string? lastId, CancellationToken cancellation)
    {
        await WriteAsync(stream, "HTTP/1.1 200 OK\r\nContent-Type: text/event-stream; charset=utf-8\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n"u8.ToArray(), cancellation);
        string session = state.SessionId.ToString("D");
        long cursor = -1;
        if (lastId?.StartsWith(session + ":", StringComparison.Ordinal) == true
            && long.TryParse(lastId.AsSpan(session.Length + 1), NumberStyles.None, CultureInfo.InvariantCulture, out long last))
            cursor = last;

        while (!cancellation.IsCancellationRequested)
        {
            var (events, changed, reset) = state.ReadAfter(cursor);
            if (reset)
            {
                var current = state.Current();
                cursor = current.Sequence;
                await WriteEventAsync(stream, session, cursor, "snapshot", new { sessionId = state.SessionId, current.Snapshot }, cancellation);
                continue;
            }
            foreach (GameEvent item in events)
            {
                await WriteEventAsync(stream, session, item.Sequence, item.Type, item, cancellation);
                cursor = item.Sequence;
            }
            try { await changed.WaitAsync(TimeSpan.FromSeconds(10), cancellation); }
            catch (TimeoutException) { await WriteAsync(stream, ": heartbeat\n\n"u8.ToArray(), cancellation); }
        }
    }

    private static Task WriteEventAsync(NetworkStream stream, string session, long sequence, string type, object data, CancellationToken cancellation)
    {
        string json = JsonSerializer.Serialize(data, BridgeState.Json);
        return WriteAsync(stream, Encoding.UTF8.GetBytes($"id: {session}:{sequence}\nevent: {type}\ndata: {json}\n\n"), cancellation);
    }

    private static async Task WriteAsync(NetworkStream stream, byte[] bytes, CancellationToken cancellation)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        await stream.WriteAsync(bytes, deadline.Token);
    }

    private static async Task ReplyAsync(NetworkStream stream, int code, object data, CancellationToken cancellation)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(data, BridgeState.Json);
        string header = $"HTTP/1.1 {code} {(HttpStatusCode)code}\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n";
        await WriteAsync(stream, Encoding.ASCII.GetBytes(header), cancellation);
        await WriteAsync(stream, body, cancellation);
    }

    public void Dispose()
    {
        stopping.Cancel();
        listener.Stop();
        stopping.Dispose();
    }
}
