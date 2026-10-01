using Kst.Application.Mps;
using Kst.Application.OpenOrders;
using Kst.Application.Tests.Mps;
using Kst.Application.Tests.PartDetail;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;
using Kst.Infrastructure.OpenOrders;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kst.Application.Tests.OpenOrders;

public sealed class OpenOrdersQxtendServiceTests
{
    private readonly Guid _id = Guid.NewGuid();
    private readonly InMemoryMpsSnapshotStore _mps = new();
    private readonly InMemoryOpenOrdersSnapshotStore _reports = new();
    private readonly SnapshotId _mpsId = SnapshotId.New();
    private readonly SnapshotId _reportId = SnapshotId.New();
    private static readonly OpenOrderEditableValues Original = new(null, null, null, null, 5m, 1m);
    private static readonly OpenOrderLineKey Key = new("TEST", "SO-1", 1);
    private readonly OpenOrderProposal _proposal = new(Key, "SW", "P-1", Original,
        Original with { OrderQty = 2m }, "Planning");

    private OpenOrdersQxtendService Service(Func<string, IReadOnlyList<OpenOrderLineKey>, CancellationToken,
        Task<IReadOnlyList<OpenOrderCurrentLine>>> read)
    {
        var workspace = new WorkspaceAssignment(_id, "Test", "SW", null, null, ["CONFIGURED"], false, null, true, 0);
        var scope = new MpsSnapshot(_mpsId, DateTimeOffset.UtcNow, "SW", [new MpsResolvedPart("P-1", null)], []);
        _mps.SetLoaded(_id, scope);
        var line = new OpenOrderLine(Key, "P-1", "SW", null, null, 1m, Original, null, null, null,
            null, null, null, "", null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, false);
        _reports.Set(_id, new(new(_reportId, _mpsId, DateTimeOffset.UtcNow, _id, "SW", [line]), false, null));
        return new(new FakeWorkspaceConfigurationService(workspace), _mps, _reports,
            new DelegateOpenOrderCurrentLineReader(read), NullLogger<OpenOrdersQxtendService>.Instance);
    }

    [Fact]
    public async Task OnlyChangedKeyIsReadAndEqualityToShippedIsValid()
    {
        var service = Service((site, keys, _) =>
        {
            Assert.Equal("SW", site);
            Assert.Equal([Key], keys);
            return Task.FromResult<IReadOnlyList<OpenOrderCurrentLine>>([new(Key, "SW", "P-1", 2m, Original)]);
        });
        Assert.Equal(QxtendOutcome.Ready, (await service.ValidateAsync(_id, _mpsId, _reportId, [_proposal], default)).Outcome);
    }

    [Fact]
    public async Task FailClosedOnScopeSourceAndInputConflicts()
    {
        OpenOrderCurrentLine row = new(Key, "SW", "P-1", 2m, Original);
        var service = Service((_, _, _) => Task.FromResult<IReadOnlyList<OpenOrderCurrentLine>>([row]));
        async Task<QxtendOutcome> Check(params OpenOrderProposal[] p) =>
            (await service.ValidateAsync(_id, _mpsId, _reportId, p, default)).Outcome;
        Assert.Equal(QxtendOutcome.Invalid, await Check(_proposal, _proposal));
        Assert.Equal(QxtendOutcome.Invalid, await Check(_proposal with { ReasonCode = null }));
        Assert.Equal(QxtendOutcome.Invalid, await Check(_proposal with { ReasonCode = "unknown" }));
        Assert.Equal(QxtendOutcome.Invalid, await Check(_proposal with { Proposed = Original }));
        Assert.Equal(QxtendOutcome.Missing, (await service.ValidateAsync(Guid.NewGuid(), _mpsId, _reportId, [_proposal], default)).Outcome);
        Assert.Equal(QxtendOutcome.Conflict, (await service.ValidateAsync(_id, _mpsId, SnapshotId.New(), [_proposal], default)).Outcome);
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal with { Site = "OTHER" }));
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal with { ItemNumber = "OTHER" }));
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal with { Original = Original with { Price = 2m } }));
        row = row with { ShippedQty = 3m };
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal));
        row = row with { ShippedQty = 2m, Values = Original with { Price = 2m } };
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal));
        row = row with { Values = Original, ItemNumber = "OTHER" };
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal));
        row = row with { ItemNumber = "P-1", Site = "OTHER" };
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal));
        row = row with { Site = "SW", Values = Original with { OrderQty = 2m } };
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal));
        _reports.Set(_id, _reports.Get(_id)! with { IsStale = true });
        Assert.Equal(QxtendOutcome.Conflict, await Check(_proposal));
    }

    [Fact]
    public async Task MissingDuplicateAndUnavailableSourceNeverAuthorize()
    {
        var missing = Service((_, _, _) => Task.FromResult<IReadOnlyList<OpenOrderCurrentLine>>([]));
        Assert.Equal(QxtendOutcome.Missing, (await missing.ValidateAsync(_id, _mpsId, _reportId, [_proposal], default)).Outcome);
        var duplicate = Service((_, _, _) => Task.FromResult<IReadOnlyList<OpenOrderCurrentLine>>([
            new(Key, "SW", "P-1", 1m, Original), new(Key, "SW", "P-1", 1m, Original)]));
        Assert.Equal(QxtendOutcome.Conflict, (await duplicate.ValidateAsync(_id, _mpsId, _reportId, [_proposal], default)).Outcome);
        var failed = Service((_, _, _) => throw new InvalidOperationException("Sensitive source failure"));
        Assert.Equal(QxtendOutcome.Unavailable, (await failed.ValidateAsync(_id, _mpsId, _reportId, [_proposal], default)).Outcome);
    }
}
