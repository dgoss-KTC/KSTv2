using System.Globalization;
using Kst.Api.Dtos;
using Kst.Application.PartInfo;
using Kst.Domain.PartDetail;

namespace Kst.Api.Endpoints;

public static class PartInfoEndpoints
{
    public static void MapPartInfoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports/part-info").WithTags("Reports");
        group.MapGet("/spec", () => new PartInfoSpecDto(PartInfoService.Sites, PartInfoService.MaximumPartLength))
            .WithName("GetPartInfoSpec");
        group.MapGet("/inventory-locations", async (string site, string part, PartInfoService service, CancellationToken ct) =>
        {
            try
            {
                var normalizedPart = PartInfoService.Validate(site, part);
                var result = await service.ReadLocationsAsync(site, normalizedPart, ct);
                return Results.Ok(new PartInfoLocationsDto(site, normalizedPart, Section(result,
                    v => (IReadOnlyList<InventoryLocationRowDto>)v.Select(l => new InventoryLocationRowDto(l.RowId, l.Location, l.LotSerial, l.Status, l.QuantityOnHand)).ToArray())));
            }
            catch (ArgumentException) { return Results.Problem("Enter one exact part number and a supported site.", statusCode: 400); }
        }).WithName("RefreshPartInfoLocations").Produces<PartInfoLocationsDto>().ProducesProblem(400);
        group.MapGet("", async (string site, string part, PartInfoService service, CancellationToken ct) =>
        {
            try
            {
                var report = await service.LookupAsync(site, part, DateOnly.FromDateTime(DateTime.Now), ct);
                return report is null ? Results.Problem(title: "Part not found", detail: "No part master exists in the selected site's domain.", statusCode: 404)
                    : Results.Ok(Map(report));
            }
            catch (ArgumentException) { return Results.Problem("Enter one exact part number (up to 256 characters) and a supported site.", statusCode: 400); }
            catch (OperationCanceledException) { throw; }
            catch { return Results.Problem("Part information could not be acquired. Retry the lookup.", statusCode: 503); }
        }).WithName("LookupPartInfo").Produces<PartInfoResponseDto>().ProducesProblem(400).ProducesProblem(404).ProducesProblem(503);
    }

    private static string? Decimal(decimal? value) => value?.ToString(CultureInfo.InvariantCulture);
    private static PartInfoSectionDto<TOut> Section<TIn, TOut>(PartInfoSection<TIn> section, Func<TIn, TOut> map) =>
        new(section.Value is null ? default : map(section.Value), section.AcquiredAtUtc, section.Error);

    private static PartInfoResponseDto Map(PartInfoReport r)
    {
        var m = r.Master;
        return new(r.Site, r.Part, new(m.Description, m.PartStatusCode, PartStatusMap.Describe(m.PartStatusCode), m.IosCode,
            Decimal(m.StandardCost), Decimal(m.Qctc), m.TimeFence, Decimal(m.SafetyTime), Decimal(m.SafetyStock), m.BuyerPlanner,
            m.PurchaseLeadTimeDays, m.InspectionLeadTimeDays, m.CumulativeLeadTimeDays, Decimal(m.MinimumOrderQuantity), Decimal(m.OrderMultiple)),
            Section(r.Inventory, v => new PartInfoInventoryDto(Decimal(v.NetQuantityOnHand)!, Decimal(v.NonNetQuantityOnHand)!)),
            Section(r.Locations, v => (IReadOnlyList<InventoryLocationRowDto>)v.Select(l => new InventoryLocationRowDto(l.RowId, l.Location, l.LotSerial, l.Status, l.QuantityOnHand)).ToArray()),
            Section(r.Alternates, v => (IReadOnlyList<ApprovedVendorDto>)v.Select(a => new ApprovedVendorDto(a.Supplier, a.VendorName, a.SupplierItem, a.ManufacturerPart)).ToArray()),
            Section(r.Context, v => new PartInfoContextDto(v.ManufacturerItem, v.EffectivePmCode, v.ManufacturingLeadWorkingDays)),
            Section(r.Orders, v => (IReadOnlyList<PartInfoOrderDto>)v.Select(l => new PartInfoOrderDto(l.PoNumber, l.PoLine, l.DueDate,
                Decimal(l.OpenQuantity)!, l.Confirmed, l.SupplierIdentifier, l.SupplierDisplay, l.BuyerDisplay, l.ManufacturerItem, l.TrackingInfo)).ToArray()),
            new(r.Comment.Value?.Text, r.Comment.AcquiredAtUtc, r.Comment.Error),
            Section(r.Suppliers, v => (IReadOnlyList<PartInfoSupplierDto>)v.Select(s => new PartInfoSupplierDto(s.Identifier, s.Display, s.IsCreditHold, s.IsCia)).ToArray()),
            r.Comment.Value?.RecordedAt, r.Comment.Value?.Author);
    }
}
