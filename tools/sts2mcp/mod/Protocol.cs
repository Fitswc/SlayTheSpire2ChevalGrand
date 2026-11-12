using System.Text.Json;

namespace STS2MCP;

internal sealed record ActionView(string Id, string Kind, string Label, object? Details = null, bool AcceptsSeed = false);
internal sealed record ActionRequest(Guid SessionId, Guid RequestId, long Revision, string ActionId, string? Seed);
internal sealed record ActionResult(Guid RequestId, string Status, long Revision, string? Error = null);
internal sealed record Snapshot(Guid SessionId, long Revision, JsonElement State);
internal sealed record GameEvent(long Sequence, string Type, DateTimeOffset Time, object Data);

internal sealed class BridgeState
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public Guid SessionId { get; } = Guid.NewGuid();

    private readonly object gate = new();
    private readonly Queue<GameEvent> events = new();
    private readonly Dictionary<Guid, (ActionRequest Request, ActionResult Result)> requests = new();
    private TaskCompletionSource changed = NewSignal();
    private ActionRequest? queued;
    private Guid? inFlight;
    private Snapshot? snapshot;
    private string? lastState;
    private long sequence;
    private long revision;
    private const int EventCapacity = 256;
    // Keep all request IDs for the lifetime of the bridge so delayed retries cannot execute twice.
    private const int RequestCapacity = 100_000;

    public Snapshot? Snapshot
    {
        get { lock (gate) return snapshot; }
    }

    public long PublishState(object value)
    {
        string json = JsonSerializer.Serialize(value, Json);
        lock (gate)
        {
            if (json == lastState)
                return revision;
            lastState = json;
            snapshot = new Snapshot(SessionId, ++revision, JsonSerializer.Deserialize<JsonElement>(json));
            PublishLocked("state", snapshot);
            return revision;
        }
    }

    public (int Code, ActionResult Result) Submit(ActionRequest request)
    {
        lock (gate)
        {
            ActionResult Reject(string error) => new(request.RequestId, "rejected", revision, error);
            if (request.SessionId != SessionId)
                return (409, Reject("stale_session"));
            if (requests.TryGetValue(request.RequestId, out var existing))
                return existing.Request == request ? (200, existing.Result) : (409, Reject("request_id_conflict"));
            if (snapshot is null)
                return (503, Reject("game_not_ready"));
            if (request.Revision != revision)
                return (409, Reject("stale_revision"));
            if (inFlight.HasValue)
                return (409, Reject("action_in_progress"));
            if (requests.Count >= RequestCapacity)
                return (503, Reject("request_capacity_reached"));

            var result = new ActionResult(request.RequestId, "queued", revision);
            requests.Add(request.RequestId, (request, result));
            queued = request;
            inFlight = request.RequestId;
            return (202, result);
        }
    }

    public ActionRequest? Take()
    {
        lock (gate)
        {
            ActionRequest? request = queued;
            queued = null;
            return request;
        }
    }

    public void Finish(ActionRequest request, string status, string? error = null)
    {
        lock (gate)
        {
            if (inFlight != request.RequestId)
                return;
            var result = new ActionResult(request.RequestId, status, revision, error);
            requests[request.RequestId] = (request, result);
            inFlight = null;
            PublishLocked("action", new { source = "mcp", request.ActionId, request.Seed, result });
        }
    }

    public ActionResult? GetResult(Guid id)
    {
        lock (gate)
            return requests.TryGetValue(id, out var entry) ? entry.Result : null;
    }

    public void Observe(ActionView action, string status)
    {
        lock (gate)
            PublishLocked("action", new { source = "manual", revision, action, status });
    }

    public (long Sequence, Snapshot? Snapshot) Current()
    {
        lock (gate) return (sequence, snapshot);
    }

    public (GameEvent[] Events, Task Changed, bool Reset) ReadAfter(long cursor)
    {
        lock (gate)
        {
            bool reset = cursor > sequence || cursor < sequence - events.Count;
            return (reset ? [] : events.Where(e => e.Sequence > cursor).ToArray(), changed.Task, reset);
        }
    }

    private void PublishLocked(string type, object data)
    {
        events.Enqueue(new GameEvent(++sequence, type, DateTimeOffset.UtcNow, data));
        if (events.Count > EventCapacity)
            events.Dequeue();
        changed.TrySetResult();
        changed = NewSignal();
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
