using System.Data;
using Dapper;
using Kst.Domain.ComponentOrders;
using Kst.Domain.Mps;
using Kst.Integrations.Qad.ComponentOrders;

namespace Kst.Integrations.Qad.Tests.ComponentOrders;

public sealed class QadComponentOrderReaderTests
{
    [Theory]
    [InlineData(0, 0, "")]
    [InlineData(1, 1, "1")]
    [InlineData(250, 1, "250")]
    [InlineData(251, 2, "250,1")]
    [InlineData(818, 4, "250,250,250,68")]
    public void ComponentOrderBatching_Uses_ReaderLocal_250ComponentPartitions(
        int componentCount,
        int expectedBatchCount,
        string expectedBatchSizes)
    {
        var components = Enumerable.Range(1, componentCount).Select(index => $"C{index}").ToList();

        var batches = MpsPartBatcher.Batch(components, QadComponentOrderReader.MaxComponentOrderBatchSize);

        Assert.Equal(250, QadComponentOrderReader.MaxComponentOrderBatchSize);
        Assert.Equal(expectedBatchCount, batches.Count);
        Assert.Equal(expectedBatchSizes, string.Join(',', batches.Select(batch => batch.Count)));
        Assert.Equal(components, batches.SelectMany(batch => batch));
    }

    [Fact]
    public void BuildBatchQuery_Is_Parameterized_And_Scoped_To_Domain_Site_And_ComponentScope()
    {
        var (sql, parameters) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1", "C2"], new DateOnly(2026, 9, 10));

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal(new DateTime(2026, 9, 10), parameters.Get<DateTime>("Today"));
        Assert.Equal("C1", parameters.Get<string>("Part0"));
        Assert.Equal("C2", parameters.Get<string>("Part1"));

        // The component scope is a VALUES CTE; part values never appear in the SQL text.
        Assert.Contains("WITH ScopeParts (PartNumber) AS", sql);
        Assert.Contains("(VALUES (@Part0), (@Part1)) AS Parts (PartNumber)", sql);
        Assert.DoesNotContain("C1", sql);
        Assert.DoesNotContain("C2", sql);

        Assert.Contains("pod.pod_domain = @Domain", sql);
        Assert.Contains("pod.pod_site = @Site", sql);
        Assert.Contains("pod.pod_part IN (SELECT PartNumber FROM ScopeParts)", sql);
    }

    [Fact]
    public void BuildBatchQuery_Qualifies_Conventional_Open_Lines_Without_A_PoStat_Predicate()
    {
        var (sql, _) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 10));

        Assert.Contains("LOWER(ISNULL(pod.pod_status, '')) NOT IN ('c', 'x')", sql);
        Assert.Contains("pod.pod_qty_ord - pod.pod_qty_rcvd > 0", sql);
        // po_mstr is joined for identity only; no master-status predicate may be introduced.
        Assert.DoesNotContain("po_stat", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBatchQuery_Joins_Vendor_By_Addr_And_Domain_And_Uses_SupplierDisplay_For_Risk_Enrichment()
    {
        var (sql, _) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 10));

        Assert.Contains("LEFT JOIN qadpro2.dbo.vd_mstr AS vd", sql);
        Assert.Contains("vd.vd_addr = pl.VendorCode", sql);
        Assert.Contains("vd.vd_domain = @Domain", sql);
        Assert.Contains("ISNULL(vd.vd_sort, pl.VendorCode) AS SupplierDisplay", sql);
        Assert.Contains("ISNULL(vd.vd_sort, pl.VendorCode) AS SupplierIdentifier", sql);
        Assert.DoesNotContain("po.po_vend AS SupplierIdentifier", sql);
    }

    [Fact]
    public void BuildBatchQuery_Resolves_Buyer_In_Workspace_Domain_With_FieldDiscriminator_And_No_CrossDomainFallback()
    {
        var (sql, _) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 10));

        // Site buyer wins when non-blank; otherwise the master buyer — each with its own code_mstr discriminator.
        Assert.Contains("THEN 'ptp_buyer'", sql);
        Assert.Contains("ELSE 'pt_buyer' END AS BuyerField", sql);
        Assert.Contains("LEFT JOIN qadpro2.dbo.code_mstr AS cm", sql);
        Assert.Contains("cm.code_domain = @Domain", sql);
        Assert.Contains("cm.code_fldname = pl.BuyerField", sql);
        Assert.Contains("cm.code_value = pl.BuyerCode", sql);
        // No cross-domain fallback: the code lookup is pinned to the workspace domain.
        Assert.DoesNotContain("cm.code_domain <> ", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBatchQuery_Uses_Accepted_Effective_Kss_Relationship_Scoped_To_ComponentScope()
    {
        var (sql, _) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 10));

        Assert.Contains("KssComponents AS", sql);
        Assert.Contains("pod2.pod_part IN (SELECT PartNumber FROM ScopeParts)", sql);
        // Unbracketed column spelling matches the accepted Stage 9 KSS reader (live-validated).
        Assert.Contains("(pod2.pod_end_eff##1 IS NULL OR pod2.pod_end_eff##1 >= @Today)", sql);
        Assert.Contains("po2.po_sched = 1", sql);
        Assert.Contains("(po2.po_eff_to IS NULL OR po2.po_eff_to >= @Today)", sql);
        // KSS is an indicator on qualifying lines only — never a second population source.
        Assert.Contains("LEFT JOIN KssComponents AS kc", sql);
    }

    [Fact]
    public void BuildBatchQuery_Keeps_Missing_PartMaster_As_Data_Via_LeftJoin_And_Falls_Back_For_LeadTime_Buyer()
    {
        var (sql, _) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 10));

        // A missing pt_mstr row must not drop the PO line: master joins are LEFT.
        Assert.Contains("LEFT JOIN qadpro2.dbo.pt_mstr AS pm", sql);
        Assert.Contains("LEFT JOIN qadpro2.dbo.ptp_det AS ptp", sql);
        Assert.DoesNotContain("INNER JOIN qadpro2.dbo.pt_mstr", sql, StringComparison.OrdinalIgnoreCase);

        // Lead time: site value with master fallback; buyer: site code non-blank wins, else master.
        Assert.Contains("ISNULL(ptp.ptp_pur_lead, pm.pt_pur_lead) AS LeadTimeDays", sql);
        Assert.Contains("LTRIM(RTRIM(ISNULL(ptp.ptp_buyer, ''))) <> '' THEN ptp.ptp_buyer ELSE pm.pt_buyer END AS BuyerCode", sql);

        // Line-level confirmation only — the master confirm flag must never be substituted.
        Assert.Contains("pod.pod__log01 AS IsConfirmed", sql);
        Assert.DoesNotContain("po_confirm", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildBatchQuery_Projects_The_Accepted_Field_Contract()
    {
        var (sql, _) = QadComponentOrderReader.BuildBatchQuery("KTC", "SW", ["C1"], new DateOnly(2026, 9, 10));

        Assert.Contains("pod.pod_part AS ComponentPart", sql);
        Assert.Contains("pm.pt_desc1 AS Description", sql);
        Assert.Contains("po.po_nbr AS PoNumber", sql);
        Assert.Contains("pod.pod_line AS PoLine", sql);
        Assert.Contains("pod.pod_due_date AS DueDate", sql);
        Assert.Contains("pod.pod_qty_ord - pod.pod_qty_rcvd AS OpenQuantity", sql);
        Assert.Contains("pod.pod_vpart AS ManufacturerItem", sql);
        Assert.Contains("pod.pod__chr06 AS TrackingInfo", sql);
        Assert.Contains("ISNULL(vd.vd_sort, pl.VendorCode) AS SupplierIdentifier", sql);
        Assert.Contains("CAST(CASE WHEN kc.ComponentPart IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS IsKss", sql);
    }

    [Fact]
    public void Normalize_Keeps_Raw_OpenQuantity_And_Converts_DueDate()
    {
        var result = QadComponentOrderReader.Normalize(new QadComponentOrderLineRawRow
        {
            ComponentPart = "C1",
            Description = "Hex bolt M8",
            LeadTimeDays = 14,
            PoNumber = "2076185",
            PoLine = 3,
            DueDate = new DateTime(2026, 9, 30),
            OpenQuantity = 12.5m,
            IsConfirmed = true,
            SupplierDisplay = "ACME Industrial",
            BuyerDisplay = "jharroun@KTC",
            ManufacturerItem = "VB-8841",
            IsKss = false,
            TrackingInfo = "  PO2076185-3  ",
            SupplierIdentifier = "ACME Industrial",
        });

        Assert.Equal("C1", result.ComponentPart);
        Assert.Equal(new DateOnly(2026, 9, 30), result.DueDate);
        Assert.Equal(12.5m, result.OpenQuantity); // raw database value — no UOM conversion
        Assert.True(result.Confirmed);
        Assert.False(result.IsKss);
        Assert.Equal("PO2076185-3", result.TrackingInfo); // trimmed
        Assert.Equal("ACME Industrial", result.SupplierIdentifier); // SupplierDisplay is the exact Shortages risk key
    }

    [Fact]
    public void Normalize_Handles_NullDueDate_MissingMaster_And_BlankText()
    {
        var result = QadComponentOrderReader.Normalize(new QadComponentOrderLineRawRow
        {
            ComponentPart = "C2",
            Description = null, // missing part-master row is legitimate data → Missing master data display state
            LeadTimeDays = null,
            PoNumber = "2076186",
            PoLine = 1,
            DueDate = null,
            OpenQuantity = 4m,
            IsConfirmed = false,
            SupplierDisplay = "VEND-9", // no vd_mstr match → raw supplier code identity
            BuyerDisplay = "   ",
            ManufacturerItem = "",
            IsKss = true,
            TrackingInfo = null,
        });

        Assert.Null(result.DueDate);
        Assert.Null(result.Description);
        Assert.Null(result.LeadTimeDays); // zero/null resolved days display as "-" downstream
        Assert.False(result.Confirmed);
        Assert.Equal("VEND-9", result.SupplierDisplay);
        Assert.Null(result.BuyerDisplay); // blank buyer normalizes to null → blank cell, never fabricated
        Assert.Null(result.ManufacturerItem);
        Assert.True(result.IsKss);
    }

    [Fact]
    public void Normalize_Keeps_NullConfirmation_Distinct_From_No()
    {
        var result = QadComponentOrderReader.Normalize(new QadComponentOrderLineRawRow
        {
            ComponentPart = "C3",
            Description = null,
            LeadTimeDays = 0,
            PoNumber = "2076187",
            PoLine = 1,
            DueDate = null,
            OpenQuantity = 1m,
            IsConfirmed = null,
            SupplierDisplay = "ACME",
            BuyerDisplay = null,
            ManufacturerItem = null,
            IsKss = false,
            TrackingInfo = null,
        });

        Assert.Null(result.Confirmed); // renders blank — never substituted with a fabricated value
    }

    [Fact]
    public void QadComponentOrderLineRawRow_Materializes_ViaDapperRowParser_AgainstLiveShapedColumns()
    {
        // Exercises Dapper's actual reflection-based row parser (SqlMapper.GetRowParser<T>) against a
        // real IDataReader whose column names/CLR types mirror the live QAD outer-query projection —
        // the exact materialization path that failed against real QAD data despite passing synthetic
        // construction-based tests. A DataTableReader is a genuine IDataReader, not a hand-built object.
        using var table = new DataTable();
        table.Columns.Add("ComponentPart", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("LeadTimeDays", typeof(int));
        table.Columns.Add("PoNumber", typeof(string));
        table.Columns.Add("PoLine", typeof(int));
        table.Columns.Add("DueDate", typeof(DateTime));
        table.Columns.Add("OpenQuantity", typeof(decimal));
        table.Columns.Add("IsConfirmed", typeof(bool));
        table.Columns.Add("SupplierDisplay", typeof(string));
        table.Columns.Add("BuyerDisplay", typeof(string));
        table.Columns.Add("ManufacturerItem", typeof(string));
        table.Columns.Add("IsKss", typeof(bool));
        table.Columns.Add("TrackingInfo", typeof(string));

        var row = table.NewRow();
        row["ComponentPart"] = "C1";
        row["Description"] = "Hex bolt M8";
        row["LeadTimeDays"] = 14;
        row["PoNumber"] = "2076185";
        row["PoLine"] = 3;
        row["DueDate"] = new DateTime(2026, 9, 30);
        row["OpenQuantity"] = 12.5m;
        row["IsConfirmed"] = true;
        row["SupplierDisplay"] = "ACME Industrial";
        row["BuyerDisplay"] = DBNull.Value; // representative of a blank/unresolved buyer display
        row["ManufacturerItem"] = "VB-8841";
        row["IsKss"] = true;
        row["TrackingInfo"] = DBNull.Value;
        table.Rows.Add(row);

        using IDataReader reader = new DataTableReader(table);
        Assert.True(reader.Read());
        var parser = reader.GetRowParser<QadComponentOrderLineRawRow>();
        var raw = parser(reader);

        Assert.Equal("C1", raw.ComponentPart);
        Assert.Equal("Hex bolt M8", raw.Description);
        Assert.Equal(14, raw.LeadTimeDays);
        Assert.Equal("2076185", raw.PoNumber);
        Assert.Equal(3, raw.PoLine);
        Assert.Equal(new DateTime(2026, 9, 30), raw.DueDate);
        Assert.Equal(12.5m, raw.OpenQuantity);
        Assert.True(raw.IsConfirmed);
        Assert.Equal("ACME Industrial", raw.SupplierDisplay);
        Assert.Null(raw.BuyerDisplay);
        Assert.Equal("VB-8841", raw.ManufacturerItem);
        Assert.True(raw.IsKss);
        Assert.Null(raw.TrackingInfo);
    }
}
