using System.Diagnostics;
using Dapper;
using Kst.Domain.Mps;
using Kst.Domain.OpenOrders;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;
using Microsoft.Extensions.Logging;

namespace Kst.Integrations.Qad.OpenOrders;

/// <summary>One bounded, parameterized report query per MPS parent batch; no per-line enrichment reads.</summary>
public sealed class QadOpenOrdersReader(QadConnectionOptions options, ILogger<QadOpenOrdersReader> logger)
{
    public async Task<IReadOnlyList<OpenOrderLine>> ReadAsync(
        string site, IReadOnlyList<string> parents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var batches = MpsPartBatcher.Batch(parents, options.MaxPartBatchSize);
        if (batches.Count == 0) return [];
        if (!options.IsConfigured) throw new InvalidOperationException("QAD connection is not configured.");
        var domain = QadSiteDomainMap.Resolve(site);
        await using var connection = await QadConnectionFactory.OpenAsync(options, cancellationToken);
        var result = new List<OpenOrderLine>();
        for (var i = 0; i < batches.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            var (sql, parameters) = QadOpenOrderQueryContract.BuildBatchQuery(domain, site, batches[i]);
            var rows = await connection.QueryAsync<OpenOrderRawRow>(new CommandDefinition(
                sql, parameters, commandTimeout: options.CommandTimeoutSeconds,
                cancellationToken: cancellationToken));
            var normalized = rows.Select(Normalize).ToArray();
            result.AddRange(normalized);
            logger.LogInformation("Open Orders batch {Index}/{Count}: {Parts} parents, {Rows} lines, {ElapsedMs}ms.",
                i + 1, batches.Count, batches[i].Count, normalized.Length, watch.ElapsedMilliseconds);
        }
        return result;
    }

    public static OpenOrderLine Normalize(OpenOrderRawRow raw) => new(
        new(raw.Domain, raw.SalesOrder, raw.Line), raw.ItemNumber, raw.Site, raw.PurchaseOrder,
        raw.Stat, raw.ShippedQty,
        new(Date(raw.DueDate), Date(raw.PerformDate), Date(raw.RequiredDate), Date(raw.DockDate), raw.OrderQty, raw.Price),
        raw.Allocated, raw.Customer, raw.CustomerName, raw.Salesperson, raw.CustomerPart,
        raw.Ios, raw.LineComments ?? "", raw.LineHold, raw.Partials, raw.Picked,
        raw.Plnr, raw.ProdStat, raw.ProductLine, raw.QaHold, raw.Remarks,
        raw.Revision, raw.ShipAcct, raw.ShipTo, raw.ShipVia, raw.SiteQoh,
        raw.SoHoldStatus, raw.SoType, raw.Consignment);

    private static DateOnly? Date(DateTime? date) => date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
}

/// <summary>Integration-only QAD query projection. Extra computed SQL aliases are intentionally ignored.</summary>
public sealed class OpenOrderRawRow
{
    public string Domain { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public string SalesOrder { get; set; } = "";
    public string? PurchaseOrder { get; set; }
    public int Line { get; set; }
    public string ItemNumber { get; set; } = "";
    public string Site { get; set; } = "";
    public decimal OrderQty { get; set; }
    public decimal ShippedQty { get; set; }
    public string? Stat { get; set; }
    public decimal Price { get; set; }
    public decimal? Allocated { get; set; }
    public string? Customer { get; set; }
    public string? CustomerName { get; set; }
    public string? Salesperson { get; set; }
    public string? CustomerPart { get; set; }
    public DateTime? DockDate { get; set; }
    public string? Ios { get; set; }
    public string? LineComments { get; set; }
    public string? LineHold { get; set; }
    public bool? Partials { get; set; }
    public DateTime? PerformDate { get; set; }
    public decimal? Picked { get; set; }
    public string? Plnr { get; set; }
    public string? ProdStat { get; set; }
    public string? ProductLine { get; set; }
    public string? QaHold { get; set; }
    public string? Remarks { get; set; }
    public DateTime? RequiredDate { get; set; }
    public string? Revision { get; set; }
    public string? ShipAcct { get; set; }
    public string? ShipTo { get; set; }
    public string? ShipVia { get; set; }
    public decimal? SiteQoh { get; set; }
    public string? SoHoldStatus { get; set; }
    public string? SoType { get; set; }
    public bool? Consignment { get; set; }
}
