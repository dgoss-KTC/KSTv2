using System.Text.Json;
using Kst.Application.OpenOrders;
using Kst.Infrastructure.Configuration;

namespace Kst.Infrastructure.OpenOrders;

/// <summary>Workspace-assignment-scoped, atomic local drafts. Invalid files are retained for recovery.</summary>
public sealed class JsonOpenOrdersDraftStore(LocalAppDataPaths paths) : IOpenOrdersDraftStore
{
    private string FilePath(Guid id) => Path.Combine(paths.ConfigDirectory, $"open-orders-draft-{id:D}.json");
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public Task<bool> ExistsAsync(Guid workspaceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(FilePath(workspaceId)));
    }

    public async Task<OpenOrdersDraft?> LoadAsync(Guid workspaceId, CancellationToken ct)
    {
        var path = FilePath(workspaceId);
        if (!File.Exists(path)) return null;
        var draft = JsonSerializer.Deserialize<OpenOrdersDraft>(await File.ReadAllTextAsync(path, ct), Options);
        if (draft is null || draft.WorkspaceId != workspaceId || draft.Site is null || draft.Proposals is null ||
            draft.Proposals.Any(p => p is null || p.Key is null || p.Original is null || p.Proposed is null ||
                string.IsNullOrWhiteSpace(p.Key.Domain) || string.IsNullOrWhiteSpace(p.Key.SalesOrder) ||
                p.Site is null || p.ItemNumber is null) ||
            draft.Proposals.Select(p => p.Key).Distinct().Count() != draft.Proposals.Count)
            throw new InvalidDataException("Invalid Open Orders draft.");
        return draft;
    }

    public async Task SaveAsync(OpenOrdersDraft draft, CancellationToken ct)
    {
        paths.EnsureDirectoriesExist();
        var path = FilePath(draft.WorkspaceId);
        var temp = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(draft, Options), ct);
            File.Move(temp, path, overwrite: true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public Task DeleteAsync(Guid workspaceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        File.Delete(FilePath(workspaceId));
        return Task.CompletedTask;
    }
}
