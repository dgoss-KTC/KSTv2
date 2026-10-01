using Kst.Application.OpenOrders;
using Kst.Domain.OpenOrders;
using Kst.Infrastructure.Configuration;
using Kst.Infrastructure.OpenOrders;

namespace Kst.Application.Tests.OpenOrders;

public sealed class JsonOpenOrdersDraftStoreTests
{
    [Fact]
    public async Task IsolatesWorkspacesPreservesOriginalAndFailsClosedOnCorruption()
    {
        var root = Path.Combine(Path.GetTempPath(), $"kst-draft-test-{Guid.NewGuid():N}");
        try
        {
            var store = new JsonOpenOrdersDraftStore(new LocalAppDataPaths(root));
            var id = Guid.NewGuid(); var other = Guid.NewGuid();
            var original = new OpenOrderEditableValues(null, null, null, null, 5m, 0.0125m);
            var draft = new OpenOrdersDraft(id, "SW", Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), DateTimeOffset.UtcNow,
                [new OpenOrderProposal(new("D", "SO-1", 1), "SW", "P", original, original with { OrderQty = 2m }, "Quality")]);
            await store.SaveAsync(draft, CancellationToken.None);
            Assert.True(await store.ExistsAsync(id, CancellationToken.None));
            Assert.False(await store.ExistsAsync(other, CancellationToken.None));
            Assert.Equal(draft.Proposals[0].Original, (await store.LoadAsync(id, CancellationToken.None))!.Proposals[0].Original);
            Assert.Null(await store.LoadAsync(other, CancellationToken.None));
            await store.DeleteAsync(other, CancellationToken.None);
            Assert.NotNull(await store.LoadAsync(id, CancellationToken.None));
            var path = Path.Combine(root, "config", $"open-orders-draft-{id:D}.json");
            await File.WriteAllTextAsync(path, "not JSON");
            await Assert.ThrowsAnyAsync<Exception>(() => store.LoadAsync(id, CancellationToken.None));
            Assert.True(File.Exists(path)); // never silently reset or erase a corrupt draft
            await store.DeleteAsync(id, CancellationToken.None);
            Assert.False(await store.ExistsAsync(id, CancellationToken.None));
            Assert.Null(await store.LoadAsync(id, CancellationToken.None));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
