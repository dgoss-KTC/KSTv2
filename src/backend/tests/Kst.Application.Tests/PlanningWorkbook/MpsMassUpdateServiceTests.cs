using System.Globalization;
using Kst.Application.Mps;
using Kst.Application.PlanningWorkbook;
using Kst.Application.Tests.PartDetail;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Mps;
using Kst.Domain.PlanningWorkbook;
using Kst.Domain.Tests.PlanningWorkbook;
using Kst.Domain.Workspaces;
using Kst.Infrastructure.Mps;

namespace Kst.Application.Tests.PlanningWorkbook;

/// <summary>
/// Stage 14.6 Mass Updater export validation. Expected values come from the accepted Stage 14 fixture
/// and golden CSV. The service is constructed with only workspace, MPS snapshot and clock dependencies:
/// there is no QAD reader to fail, which the dependency test below asserts structurally.
/// </summary>
public sealed class MpsMassUpdateServiceTests
{
    private static readonly Stage14Fixture Fixture = Stage14Fixture.Load();
    private static readonly DateOnly W0914 = new(2026, 9, 14);
    private static readonly DateOnly W0921 = new(2026, 9, 21);
    private static readonly DateOnly W0928 = new(2026, 9, 28);
    private static readonly DateOnly W1005 = new(2026, 10, 5);

    private static MpsAdjustmentEntry E(string part, DateOnly week, string? text) => new(part, week, text);

    private static List<MpsAdjustmentEntry> Scenario() =>
        Fixture.Root.GetProperty("adjustmentScenario").GetProperty("adjustments").EnumerateArray()
            .Select(a => E(a.GetProperty("part").GetString()!, Stage14Fixture.Date(a.GetProperty("weekLabel")), a.GetProperty("adjustment").GetString()))
            .ToList();

    [Fact]
    public async Task Scenario_Validates_Against_The_Loaded_Snapshot_And_Serializes_To_The_Golden_File()
    {
        var h = new Harness();

        var result = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, Scenario(), default);

        Assert.Equal(MpsMassUpdateOutcome.Ready, result.Outcome);
        Assert.Equal(h.MpsId, result.MpsSnapshotId);
        Assert.Equal(Fixture.Today, result.BusinessDate);
        var golden = File.ReadAllBytes(Path.Combine(Fixture.RepositoryRoot, "docs", "reference", "Stage14Fixtures", "MpsAdjustments.expected.csv"));
        // The rows are exactly the golden file's logical rows, in its order (byte serialization: MpsMassUpdateCsvTests).
        var lines = result.Rows!.Select(r => string.Join(',', r.Part, r.Adjustment.ToString("0", CultureInfo.InvariantCulture), $"{r.WeekLabel.Month}/{r.WeekLabel.Day}/{r.WeekLabel.Year}"));
        Assert.Equal(System.Text.Encoding.UTF8.GetString(golden).Split("\r\n", StringSplitOptions.RemoveEmptyEntries), lines);
    }

    [Fact]
    public async Task Entries_Hidden_By_Horizon_View_Or_Filters_Are_Validated_Against_The_Full_Snapshot()
    {
        var h = new Harness();
        // P-1 has 60 due 2026-10-15 (week 2026-10-12), beyond the fixture's 4-week workbook horizon.
        var far = new DateOnly(2026, 10, 12);

        var valid = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", far, "-60")], default);
        Assert.Equal(MpsMassUpdateOutcome.Ready, valid.Outcome);
        Assert.Equal(new MpsMassUpdateRow("P-1", -60m, far), Assert.Single(valid.Rows!));

        // The hidden week's baseline is real (60): reducing by 61 is a negative result and rejects everything.
        var negative = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [.. Scenario(), E("P-1", far, "-61")], default);
        Assert.Equal(MpsMassUpdateOutcome.Rejected, negative.Outcome);
        Assert.Equal("negative-result", negative.IssueCode);
        Assert.Equal(1, negative.AffectedRowCount);
        Assert.Null(negative.Rows);
    }

    [Fact]
    public async Task A_Week_With_No_Supply_Has_A_Genuine_Zero_Baseline_While_Unavailable_Baselines_Are_Rejected_Not_Zeroed()
    {
        var h = new Harness();

        // P-1 week 2026-10-05 has no MPS rows: baseline is a real zero, so +20 is valid and -1 is negative.
        Assert.Equal(MpsMassUpdateOutcome.Ready, (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", W1005, "20")], default)).Outcome);
        var below = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", W1005, "-1")], default);
        Assert.Equal("negative-result", below.IssueCode);

        // Passed week and out-of-scope part have no baseline: never treated as zero.
        var passed = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", new DateOnly(2026, 9, 7), "5")], default);
        Assert.Equal("week-passed", passed.IssueCode);
        var outOfScope = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("X-9", W0921, "5")], default);
        Assert.Equal("part-out-of-scope", outOfScope.IssueCode);
    }

    [Theory]
    [InlineData("+5", "invalid-number")]
    [InlineData("1e3", "invalid-number")]
    [InlineData("1,000", "invalid-number")]
    [InlineData(" 5", "invalid-number")]
    [InlineData("-", "invalid-number")]
    [InlineData("10.5", "not-whole-number")]
    [InlineData("10.0", "not-whole-number")]
    public async Task Whole_Number_Rules_Reject_The_Whole_Export(string text, string code)
    {
        var h = new Harness();

        var result = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [.. Scenario(), E("P-2", W0921, text)], default);

        Assert.Equal(MpsMassUpdateOutcome.Rejected, result.Outcome);
        Assert.Equal(code, result.IssueCode);
        Assert.Null(result.Rows);
    }

    [Fact]
    public async Task Week_Duplicate_Falldown_And_Non_Monday_Rules_Reject_The_Whole_Export()
    {
        var h = new Harness();

        Assert.Equal("not-a-week", (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [.. Scenario(), E("P-2", new DateOnly(2026, 9, 22), "1")], default)).IssueCode);
        Assert.Equal("duplicate-identity", (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [.. Scenario(), E("p-1", W0914, "1")], default)).IssueCode);
        // A duplicate no-op identity is still a duplicate (identity is structural).
        Assert.Equal("duplicate-identity", (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [.. Scenario(), E("P-1", W0914, "0")], default)).IssueCode);
        var falldown = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [.. Scenario(), new MpsAdjustmentEntry("P-1", null, "5")], default);
        Assert.Equal("not-adjustable", falldown.IssueCode);
    }

    [Fact]
    public async Task One_Invalid_Entry_Among_Valid_Ones_Produces_No_Rows_And_Reports_Only_A_Count()
    {
        var h = new Harness();

        var result = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId,
            [.. Scenario(), E("P-1", W0928, "-150"), E("P-2", W0921, "9999999999999999999999999999999999")], default);

        Assert.Equal(MpsMassUpdateOutcome.Rejected, result.Outcome);
        Assert.Equal(2, result.AffectedRowCount);
        Assert.Null(result.Rows);
    }

    [Fact]
    public async Task Nothing_To_Export_Is_Rejected_Rather_Than_Producing_An_Empty_File()
    {
        var h = new Harness();

        var noOps = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", W0914, "0"), E("P-1", W0921, "-0"), E("P-2", W0921, "")], default);
        var none = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [], default);

        Assert.Equal(("no-adjustments", MpsMassUpdateOutcome.Rejected), (noOps.IssueCode, noOps.Outcome));
        Assert.Equal(("no-adjustments", MpsMassUpdateOutcome.Rejected), (none.IssueCode, none.Outcome));
    }

    [Fact]
    public async Task Negative_Inventory_And_Sales_Order_Shortfalls_Do_Not_Block()
    {
        var h = new Harness();

        // P-1 week 2026-09-14 holds a released work order and SO 80; reducing MPS to 0 is valid.
        var result = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", W0914, "-100")], default);

        Assert.Equal(MpsMassUpdateOutcome.Ready, result.Outcome);
    }

    [Fact]
    public async Task Workspace_Site_And_Snapshot_Identity_Are_Required()
    {
        var h = new Harness(loadMps: false);
        var entries = new[] { E("P-1", W0921, "5") };

        Assert.Equal("mps-not-loaded", (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, entries, default)).IssueCode);
        h.LoadMps(h.MpsId);
        Assert.Equal(MpsMassUpdateOutcome.Ready, (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, entries, default)).Outcome);

        Assert.Equal(MpsMassUpdateOutcome.UnknownWorkspace, (await h.Service.PrepareAsync(Guid.NewGuid(), h.Site, h.MpsId, entries, default)).Outcome);
        Assert.Equal(MpsMassUpdateOutcome.SnapshotChanged, (await h.Service.PrepareAsync(h.Id, h.Site, SnapshotId.New(), entries, default)).Outcome);
        Assert.Equal(MpsMassUpdateOutcome.SiteChanged, (await h.Service.PrepareAsync(h.Id, "OTHER", h.MpsId, entries, default)).Outcome);
        Assert.Equal(MpsMassUpdateOutcome.InvalidRequest, (await h.Service.PrepareAsync(h.Id, "", h.MpsId, entries, default)).Outcome);

        // The loaded snapshot belongs to a different site than the workspace now uses.
        h.Workspaces.Items[0] = h.Workspaces.Items[0] with { Site = "S2" };
        Assert.Equal(MpsMassUpdateOutcome.SiteChanged, (await h.Service.PrepareAsync(h.Id, "S2", h.MpsId, entries, default)).Outcome);
    }

    [Fact]
    public async Task A_Snapshot_Loaded_For_Another_Workspace_Cannot_Authorize_This_Workspace()
    {
        var a = new Harness();
        var otherId = Guid.NewGuid();
        var otherMps = SnapshotId.New();
        a.Mps.SetLoaded(otherId, new MpsSnapshot(otherMps, a.Clock.UtcNow, a.Site, Fixture.ResolvedParts, []));
        a.Workspaces.Items.Add(new WorkspaceAssignment(otherId, "Other", a.Site, null, null, ["CONFIGURED"], false, null, true, 1));

        Assert.Equal(MpsMassUpdateOutcome.SnapshotChanged,
            (await a.Service.PrepareAsync(a.Id, a.Site, otherMps, [E("P-1", W0921, "5")], default)).Outcome);
        // Overlapping part/week in the other workspace has an empty MPS: its own baseline is zero there.
        var other = await a.Service.PrepareAsync(otherId, a.Site, otherMps, [E("P-1", W0921, "5")], default);
        Assert.Equal(MpsMassUpdateOutcome.Ready, other.Outcome);
        var own = await a.Service.PrepareAsync(a.Id, a.Site, a.MpsId, [E("P-1", W0921, "-76")], default);
        Assert.Equal("negative-result", own.IssueCode); // 75.5 baseline here, not the other workspace's zero
    }

    [Theory]
    [InlineData(0)]
    [InlineData(MpsMassUpdateService.MaxPartLength + 1)]
    public async Task Malformed_Parts_And_Oversized_Requests_Are_Invalid_Requests(int partLength)
    {
        var h = new Harness();
        var part = new string('P', partLength);

        Assert.Equal(MpsMassUpdateOutcome.InvalidRequest, (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E(part, W0921, "1")], default)).Outcome);
        var tooMany = Enumerable.Range(0, MpsMassUpdateService.MaxEntries + 1).Select(i => E("P-1", W0921.AddDays(7 * i), "1")).ToList();
        Assert.Equal(MpsMassUpdateOutcome.InvalidRequest, (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, tooMany, default)).Outcome);
    }

    [Fact]
    public async Task Output_Uses_The_Retained_Part_Spelling_And_Order_And_The_Workspace_Label()
    {
        var h = new Harness();

        var result = await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("p-1", W0921, "3"), E("p-2", W0914, "4")], default);

        Assert.Equal(["P-2", "P-1"], result.Rows!.Select(r => r.Part));
        Assert.Equal("Test", result.WorkspaceLabel);
        h.Workspaces.Items[0] = h.Workspaces.Items[0] with { DisplayName = null };
        Assert.Equal(h.Site, (await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, [E("P-1", W0921, "3")], default)).WorkspaceLabel);
    }

    [Fact]
    public async Task Export_Preparation_Does_Not_Mutate_The_Loaded_Snapshot()
    {
        var h = new Harness();
        var before = h.Mps.GetState(h.Id).Snapshot!;

        await h.Service.PrepareAsync(h.Id, h.Site, h.MpsId, Scenario(), default);

        Assert.Same(before, h.Mps.GetState(h.Id).Snapshot);
        Assert.Equal(h.MpsId, h.Mps.GetState(h.Id).Snapshot!.Id);
    }

    [Fact]
    public void Export_Service_Has_No_Qad_Reader_Or_Workbook_Source_Dependency()
    {
        var parameters = typeof(MpsMassUpdateService).GetConstructors().Single().GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Equal([typeof(IWorkspaceConfigurationService), typeof(IMpsSnapshotStore), typeof(IClock)], parameters);
        Assert.DoesNotContain(typeof(MpsMassUpdateService).Assembly.GetReferencedAssemblies(),
            a => a.Name!.Contains("Qad", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(MpsMassUpdateService).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic),
            f => f.FieldType.Name.Contains("Reader", StringComparison.Ordinal) || f.FieldType == typeof(PlanningWorkbookService));
    }

    private sealed class Harness
    {
        public Harness(bool loadMps = true)
        {
            Id = Fixture.Root.GetProperty("scope").GetProperty("workspaceAssignmentId").GetGuid();
            Site = Fixture.Root.GetProperty("scope").GetProperty("site").GetString()!;
            MpsId = new SnapshotId(Fixture.LoadedSnapshotId);
            Clock.LocalNow = new DateTimeOffset(Fixture.Today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.FromHours(-7));
            Clock.UtcNow = Clock.LocalNow.ToUniversalTime();
            Workspaces = new Workspaces([new WorkspaceAssignment(Id, "Test", Site, null, null, ["CONFIGURED"], false, null, true, 0)]);
            if (loadMps) LoadMps(MpsId);
            Service = new MpsMassUpdateService(Workspaces, Mps, Clock);
        }

        public Guid Id { get; }
        public string Site { get; }
        public SnapshotId MpsId { get; }
        public FakeClock Clock { get; } = new();
        public InMemoryMpsSnapshotStore Mps { get; } = new();
        public Workspaces Workspaces { get; }
        public MpsMassUpdateService Service { get; }

        public void LoadMps(SnapshotId snapshotId) =>
            Mps.SetLoaded(Id, new MpsSnapshot(snapshotId, Clock.UtcNow, Site, Fixture.ResolvedParts,
                Fixture.MpsRows.Select(r => r with { Site = Site }).ToList()));
    }

    private sealed class Workspaces(IReadOnlyList<WorkspaceAssignment> initial) : IWorkspaceConfigurationService
    {
        public List<WorkspaceAssignment> Items { get; } = [.. initial];
        public Task<WorkspaceListResult> GetWorkspacesAsync() => Task.FromResult(new WorkspaceListResult(Items.ToList(), null));
        public Task<WorkspaceCreateResult> CreateWorkspaceAsync(CreateWorkspaceCommand command) => throw new NotSupportedException();
        public Task<WorkspaceUpdateResult> UpdateWorkspaceAsync(Guid assignmentId, CreateWorkspaceCommand command) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> ArchiveWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> RestoreWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceOperationResult> DeleteWorkspaceAsync(Guid assignmentId) => throw new NotSupportedException();
        public Task<WorkspaceReorderResult> ReorderWorkspacesAsync(ReorderWorkspacesCommand command) => throw new NotSupportedException();
        public Task ResetWorkspacesAsync() => throw new NotSupportedException();
    }
}
