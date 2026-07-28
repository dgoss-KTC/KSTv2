using Kst.Application.Snapshots;
using Kst.Application.SystemStatus;
using Kst.Domain.Common;
using Kst.Domain.Snapshots;
using Kst.Infrastructure.Clock;
using Kst.Infrastructure.Snapshots;

namespace Kst.Application.Tests;

public sealed class GetSystemStatusQueryTests
{
    private static GetSystemStatusQuery BuildQuery(
        IClock? clock = null,
        ISnapshotStore? snapshotStore = null,
        ApplicationInfo? appInfo = null,
        IReadOnlyList<DataSourceSummary>? dataSources = null)
    {
        return new GetSystemStatusQuery(
            clock ?? new SystemClock(),
            snapshotStore ?? new InMemorySnapshotStore(),
            appInfo ?? new ApplicationInfo("KST", "0.1.0", "test-instance", DateTimeOffset.Now),
            dataSources ?? []
        );
    }

    [Fact]
    public void Execute_Returns_ApplicationName()
    {
        var query = BuildQuery(appInfo: new ApplicationInfo("KST", "0.1.0", "id", DateTimeOffset.Now));

        var result = query.Execute();

        Assert.Equal("KST", result.ApplicationName);
    }

    [Fact]
    public void Execute_Returns_BackendFramework_DotNet10()
    {
        var query = BuildQuery();
        var result = query.Execute();

        Assert.Equal(".NET 10", result.BackendFramework);
    }

    [Fact]
    public void Execute_Snapshot_NotLoaded_When_Store_Is_Empty()
    {
        var query = BuildQuery();
        var result = query.Execute();

        Assert.Equal(SnapshotStatus.NotLoaded, result.Snapshot.Status);
        Assert.False(result.Snapshot.IsAvailable);
    }

    [Fact]
    public void Execute_Snapshot_IsAvailable_When_Loaded()
    {
        var store = new InMemorySnapshotStore();
        store.ReplaceSnapshot(new SnapshotInfo(
            SnapshotId.New(),
            DateTimeOffset.Now,
            SnapshotStatus.Loaded
        ));

        var query = BuildQuery(snapshotStore: store);
        var result = query.Execute();

        Assert.True(result.Snapshot.IsAvailable);
    }

    [Fact]
    public void Execute_DataSources_AreIncluded()
    {
        var sources = new List<DataSourceSummary>
        {
            new("QAD", DataSourceStatus.NotConfigured),
            new("Shortage Database", DataSourceStatus.NotConfigured)
        };

        var query = BuildQuery(dataSources: sources);
        var result = query.Execute();

        Assert.Equal(2, result.DataSources.Count);
        Assert.Contains(result.DataSources, d => d.Name == "QAD");
    }

    [Fact]
    public void Execute_CurrentTime_Is_Recent()
    {
        var before = DateTimeOffset.Now.AddSeconds(-1);
        var query = BuildQuery();
        var result = query.Execute();
        var after = DateTimeOffset.Now.AddSeconds(1);

        Assert.InRange(result.CurrentTime, before, after);
    }
}
