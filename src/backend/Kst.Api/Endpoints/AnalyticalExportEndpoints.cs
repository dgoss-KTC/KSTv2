using System.Globalization;
using Kst.Api.Dtos;
using Kst.Application.Bom;
using Kst.Application.Mps;
using Kst.Application.Workspaces;
using Kst.Domain.Common;
using Kst.Domain.Exports;
using Kst.Domain.Mps;
using Kst.Exports;

namespace Kst.Api.Endpoints;

/// <summary>Loaded-data only. No source reader or acquisition service is reachable here.</summary>
public static class AnalyticalExportEndpoints
{
    public static void MapAnalyticalExportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/reports/analytical", Prepare).WithName("PrepareAnalyticalWorkbook").WithTags("Exports")
            .Produces<AnalyticalExportPreparedDto>().ProducesProblem(400).ProducesProblem(409).ProducesProblem(422).ProducesProblem(503);
        app.MapGet("/api/v1/reports/analytical/preparations/{id:guid}/bytes", (Guid id, HttpRequest http, ComponentMrpPreparations store) =>
        {
            var owner = http.Headers["X-KST-Report-Session"].ToString();
            if (!Guid.TryParseExact(owner, "D", out _)) return Results.Problem("A transport session is required.", statusCode: 400);
            var bytes = store.Get(id, owner, "Analytical");
            return bytes is null ? Results.Problem("Preparation expired. Prepare again.", statusCode: 410)
                : Results.Bytes(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }).WithName("GetAnalyticalWorkbookBytes").WithTags("Exports").Produces(200, contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet").ProducesProblem(410);
    }

    private static async Task<IResult> Prepare(AnalyticalExportRequestDto request, HttpRequest http, IWorkspaceConfigurationService workspaces,
        IMpsSnapshotStore mps, IBomCacheStore boms, ComponentMrpPreparations preparations, IClock clock, CancellationToken ct)
    {
        var owner = http.Headers["X-KST-Report-Session"].ToString();
        if (!Guid.TryParseExact(owner, "D", out _) || request.Kind is not ("MPS" or "BOM") || request.Metadata is null || request.Sheets is null || request.FiscalLabels is null)
            return Results.Problem("Invalid analytical export request.", statusCode: 400);
        if (!await preparations.Generations.WaitAsync(0, ct)) return Results.Problem("Workbook preparation is busy. Retry.", statusCode: 503);
        try
        {
            var workspace = (await workspaces.GetWorkspacesAsync()).Workspaces.FirstOrDefault(w => w.AssignmentId == request.WorkspaceId);
            var state = mps.GetState(request.WorkspaceId);
            var snapshot = state.Snapshot;
            if (workspace is null || snapshot is null || snapshot.Id.ToString() != request.MpsSnapshotId.ToString() || workspace.Site != request.Site || snapshot.Site != request.Site)
                return Results.Problem("Workspace or loaded source changed. Prepare again from compatible loaded data.", statusCode: 409);
            var today = DateOnly.FromDateTime(clock.LocalNow.Date);
            if (request.BusinessDate != today) return Results.Problem("Business date changed. Reload the view before preparing.", statusCode: 409);
            var metadata = new List<KeyValuePair<string, string>>
            {
                new("Report", request.Kind + " analytical Excel"), new("Workspace", workspace.DisplayName ?? request.WorkspaceId.ToString()),
                new("Workspace ID", request.WorkspaceId.ToString()), new("Site", snapshot.Site), new("MPS snapshot", snapshot.Id.ToString()),
                new("MPS acquired UTC", snapshot.LoadedAt.ToString("O")), new("Exported UTC", clock.UtcNow.ToString("O")),
                new("Business date", today.ToString("yyyy-MM-dd")), new("Purpose", "Analytical only. No changes applied to QAD. Drafts and adjustments are unchanged."),
                new("Precision", "Values exceeding Excel's 15-digit numeric precision are literal text. No source text is executed as a formula.")
            };
            IReadOnlyList<AnalyticalSheet> sheets;
            if (request.Kind == "MPS")
            {
                if (request.DateBasis is not ("dueDate" or "releaseDate") || request.HorizonWeeks is < 1 or > 72 || request.FiscalLabels.Count != request.HorizonWeeks)
                    return Results.Problem("Invalid MPS selections.", statusCode: 400);
                var schedules = MpsScheduleBuilder.Build(snapshot.ResolvedParts, snapshot.SourceRows,
                    request.DateBasis == "dueDate" ? MpsDateBasis.DueDate : MpsDateBasis.ReleaseDate, request.HorizonWeeks, today);
                if (request.Parent is not null && !schedules.Any(p => p.ParentPart == request.Parent)) return Results.Problem("Selected parent is no longer in scope.", statusCode: 409);
                var selected = schedules.Where(p => request.Parent is null || p.ParentPart == request.Parent).ToArray();
                var headers = new[] { "Parent Part", "Description", "Falldown" }.Concat(Enumerable.Range(0, request.HorizonWeeks).Select(i =>
                    $"{MpsBusinessCalendar.GetWeekLabel(MpsBusinessCalendar.GetBusinessWeekStart(today)).AddDays(i * 7):yyyy-MM-dd}\n{request.FiscalLabels[i]}")).ToArray();
                var rows = new List<IReadOnlyList<AnalyticalCell>>();
                foreach (var part in selected)
                {
                    rows.Add(new AnalyticalCell[] { new(part.ParentPart), new(part.Description) }.Concat(part.Buckets.Select(b => new AnalyticalCell(Text(b.Quantity), "quantity"))).ToArray());
                }
                metadata.Add(new("Scope", request.Parent is null ? $"All {selected.Length} workspace parents (drilldown does not narrow scope)" : "Selected parent: " + request.Parent));
                metadata.Add(new("Date basis", request.DateBasis)); metadata.Add(new("Horizon weeks", request.HorizonWeeks.ToString()));
                metadata.Add(new("Source status", state.Status.ToString()));
                if (state.Status != Kst.Domain.Snapshots.SnapshotStatus.Current || request.IsStale) metadata.Add(new("Warning", "Showing loaded last-known MPS data; refresh was not performed for export."));
                metadata.Add(new("Falldown", "Always Due Date based, including in Release Date view."));
                metadata.AddRange(request.Metadata.Take(10).Select(m => new KeyValuePair<string, string>(m.Label, m.Value)));
                sheets = [new("MPS", headers, rows, 2)];
            }
            else
            {
                var cached = request.Parent is null ? null : boms.Get(request.WorkspaceId, request.Parent);
                if (cached is null || cached.Site != request.Site || cached.EffectiveDate != today || cached.EffectiveDate != request.BomEffectiveDate || cached.Bom.LoadedAtUtc != request.BomLoadedAtUtc)
                    return Results.Problem("Loaded BOM changed or is incompatible. Reload the BOM before preparing.", statusCode: 409);
                if (cached.LoadedAgainstMpsSnapshotId != snapshot.Id && !request.IsStale) return Results.Problem("BOM snapshot changed. Reload the BOM.", statusCode: 409);
                if (request.PmFilter is not ("all" or "P" or "M") || request.PhantomFilter is not ("all" or "yes" or "no")) return Results.Problem("Invalid BOM filters.", statusCode: 400);
                var lines = cached.Bom.Lines.Where(l => l.ComponentPart.Contains((request.ComponentFilter ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                    && (l.Description ?? "").Contains((request.DescriptionFilter ?? "").Trim(), StringComparison.OrdinalIgnoreCase)
                    && (request.PmFilter == "all" || l.PmCode == request.PmFilter)
                    && (request.PhantomFilter == "all" || l.IsPhantom == (request.PhantomFilter == "yes"))).ToArray();
                metadata.AddRange([new("Parent", request.Parent!), new("BOM acquired UTC", cached.Bom.LoadedAtUtc.ToString("O")),
                    new("Effective date", cached.EffectiveDate.ToString("yyyy-MM-dd")), new("Component Item filter", request.ComponentFilter ?? ""), new("Description filter", request.DescriptionFilter ?? ""),
                    new("P/M filter", request.PmFilter!), new("Phantom filter", request.PhantomFilter!), new("Structural scope", "Loaded selected-parent occurrences in original order. Level gaps and repeated inventory are intentional; no ancestors are inferred.")]);
                if (request.IsStale || cached.Bom.IsStale) metadata.Add(new("Warning", "Showing last-known BOM data. " + request.Warning));
                sheets = [new("BOM", ["Level", "Component Item", "P/M", "Phantom", "Description", "Qty Per", "Scrap (%)", "Net QOH", "Non-Net QOH"],
                    lines.Select(l => (IReadOnlyList<AnalyticalCell>)new AnalyticalCell[] { new(l.Level.ToString(), "quantity"), new(l.ComponentPart), new(l.PmCode), new(l.IsPhantom ? "Yes" : "No"), new(l.Description),
                        new(Text(l.QuantityPer), "quantity"), new(Text(l.ScrapPercentage), "quantity"), new(Text(l.NetQuantityOnHand), "quantity"), new(Text(l.NonNetQuantityOnHand), "quantity") }).ToArray(), 2)];
            }
            metadata.Add(new("Result", sheets.All(s => s.Rows.Count == 0) ? "No matching rows. Headers and metadata only." : "Complete selected loaded result; no pagination, scrolling or collapse omissions."));
            ct.ThrowIfCancellationRequested();
            var bytes = AnalyticalWorkbook.Create(new(request.Kind, metadata, sheets));
            ct.ThrowIfCancellationRequested();
            if (mps.GetState(request.WorkspaceId).Snapshot?.Id != snapshot.Id) return Results.Problem("Source changed while preparing. Prepare again.", statusCode: 409);
            var stored = preparations.Add(owner, bytes, "Analytical");
            return Results.Ok(new AnalyticalExportPreparedDto(stored.Id.ToString(), $"{request.Kind}_{request.Site}_{today:yyyy-MM-dd}_01.xlsx", stored.Expires));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (ArgumentException) { return Results.Problem("Workbook inputs exceed supported capacity or contain invalid presentation data. No workbook was prepared; nothing was truncated.", statusCode: 422); }
        catch (InvalidOperationException) { return Results.Problem("Workbook exceeds preparation capacity. No workbook was prepared.", statusCode: 422); }
        catch (Exception) { return Results.Problem("Analytical workbook preparation failed. No file was saved.", statusCode: 503); }
        finally { preparations.Generations.Release(); }
    }
    private static string Text(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string? Text(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
}
