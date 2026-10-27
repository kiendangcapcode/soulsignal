using System.Collections.Concurrent;

namespace SoulSignal.Services;

public sealed record UploadProgressSnapshot(string Phase, int? Percent, string Message);

// Small, temporary status records only. Uploaded files and song metadata stay in the normal workflow.
public sealed class UploadProgressStore
{
    private readonly ConcurrentDictionary<string, UploadProgressHandle> entries = new();
    private readonly object createLock = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public string? Start(string ownerId)
    {
        lock (createLock)
        {
            var oldest = DateTimeOffset.UtcNow - Lifetime;
            foreach (var item in entries)
                if (item.Value.UpdatedAt < oldest) entries.TryRemove(item.Key, out _);
            if (entries.Count >= 1000 || entries.Values.Count(x => x.OwnerId == ownerId && !x.IsTerminal) >= 8)
                return null;
            var id = Guid.NewGuid().ToString("N");
            entries[id] = new UploadProgressHandle(ownerId);
            return id;
        }
    }

    public UploadProgressSnapshot? Get(string id, string ownerId) => Find(id, ownerId)?.Snapshot;
    public UploadProgressSnapshot? Cancel(string id, string ownerId)
    {
        var progress = Find(id, ownerId);
        progress?.Cancel();
        return progress?.Snapshot;
    }

    public UploadProgressHandle? Claim(string? id, string ownerId)
    {
        var progress = id is null ? null : Find(id, ownerId);
        return progress?.TryClaim() == true ? progress : null;
    }

    private UploadProgressHandle? Find(string id, string ownerId) =>
        Guid.TryParseExact(id, "N", out _) && entries.TryGetValue(id, out var entry)
        && entry.OwnerId == ownerId && entry.UpdatedAt >= DateTimeOffset.UtcNow - Lifetime ? entry : null;
}

public sealed class UploadProgressHandle(string ownerId)
{
    private readonly object sync = new();
    private bool claimed;
    private readonly CancellationTokenSource cancellation = new();
    private UploadProgressSnapshot snapshot = new("waiting", null, "Đang chờ tệp tải lên.");
    private DateTimeOffset updatedAt = DateTimeOffset.UtcNow;
    public string OwnerId { get; } = ownerId;
    public CancellationToken CancellationToken => cancellation.Token;
    public DateTimeOffset UpdatedAt { get { lock (sync) return updatedAt; } }
    public UploadProgressSnapshot Snapshot { get { lock (sync) return snapshot; } }
    public bool IsTerminal { get { lock (sync) return IsTerminalPhase(snapshot.Phase); } }

    internal bool TryClaim()
    {
        lock (sync)
        {
            if (claimed || IsTerminalPhase(snapshot.Phase)) return false;
            claimed = true;
            updatedAt = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public void Report(MediaProcessingProgress progress) => Set(progress.Phase, progress.Percent, progress.Message);
    public void Saving() => Set("saving", null, "Đang lưu bài hát để gửi admin xét duyệt.");
    public void Complete() => Set("completed", 100, "Đã xử lý xong. Bài hát đang chờ admin duyệt.");
    public void Fail(string message) => Set("failed", null, message);
    public void Cancel()
    {
        if (IsTerminal) return;
        Set("cancelled", null, "Đã hủy xử lý nhạc.");
        cancellation.Cancel();
    }

    private void Set(string phase, int? percent, string message)
    {
        lock (sync)
        {
            if (IsTerminalPhase(snapshot.Phase)) return;
            if (phase == "encoding" && percent.HasValue)
                percent = Math.Max(snapshot.Phase == "encoding" ? snapshot.Percent ?? 0 : 0, Math.Clamp(percent.Value, 0, 99));
            snapshot = new(phase, percent, message);
            updatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static bool IsTerminalPhase(string phase) => phase is "completed" or "failed" or "cancelled";
}
