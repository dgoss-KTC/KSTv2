using Kst.Integrations.Qad.Bom;

namespace Kst.Integrations.Qad.Tests.Bom;

public sealed class QadBomReaderTests
{
    private static readonly DateOnly Effective = new(2026, 7, 21);

    // ===== SQL shape (BuildQuery) =====

    [Fact]
    public void BuildQuery_Adds_Domain_Site_Parent_And_Midnight_EffectiveDate_Parameters()
    {
        var (sql, parameters) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("SW", parameters.Get<string>("Site"));
        Assert.Equal("PARENT-1", parameters.Get<string>("ParentPart"));
        Assert.Equal(new DateTime(2026, 7, 21, 0, 0, 0), parameters.Get<DateTime>("EffectiveDate"));
        Assert.Contains("@ParentPart", sql);
        Assert.Contains("@EffectiveDate", sql);
        Assert.Contains("@Site", sql);
    }

    [Fact]
    public void BuildQuery_EffectiveDate_Predicate_Present_In_Anchor_And_Recursion()
    {
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        // Open start / start <= effective date, in both the anchor and the recursive member.
        Assert.Contains("ps.ps_start IS NULL OR ps.ps_start <= @EffectiveDate", sql);
        Assert.Contains("ch.ps_start IS NULL OR ch.ps_start <= @EffectiveDate", sql);

        // Open end / end >= effective date, in both the anchor and the recursive member.
        Assert.Contains("ps.ps_end IS NULL OR ps.ps_end >= @EffectiveDate", sql);
        Assert.Contains("ch.ps_end IS NULL OR ch.ps_end >= @EffectiveDate", sql);
    }

    [Fact]
    public void BuildQuery_Recursion_Joins_Frontier_Component_To_Child_Parent_In_Domain()
    {
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.Contains("FROM qadpro2.dbo.ps_mstr AS ps", sql);
        Assert.Contains("ps.ps_par = @ParentPart", sql);
        Assert.Contains("ps.ps_domain = @Domain", sql);
        Assert.Contains("INNER JOIN BomStructure AS frontier", sql);
        Assert.Contains("frontier.ComponentPart = ch.ps_par", sql);
        Assert.Contains("ch.ps_domain = @Domain", sql);
    }

    [Fact]
    public void BuildQuery_Has_No_Operation_Filter()
    {
        // Stage 8 has no operation-range UI; no ps_op condition may hide effective rows.
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.DoesNotContain("ps_op", sql);
    }

    [Fact]
    public void BuildQuery_Closure_Distinct_Includes_Relationship_Identity_And_Is_Not_Aggregation()
    {
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        // Exactly one DISTINCT — the approved closure collapse of duplicate path copies, whose
        // selected identity includes the relationship OID so distinct relationships never merge.
        Assert.Equal(1, CountOccurrences(sql, "SELECT DISTINCT"));
        Assert.Contains("b.OidPsMstr", sql);
        Assert.DoesNotContain("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SUM(", sql);
        Assert.DoesNotContain("COUNT(", sql);
    }

    [Fact]
    public void BuildQuery_Joins_PartMaster_On_Domain_And_Part_Without_PtSite()
    {
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.Contains("LEFT JOIN qadpro2.dbo.pt_mstr AS pt", sql);
        Assert.Contains("pt.pt_domain = @Domain", sql);
        Assert.Contains("pt.pt_part = u.ComponentPart", sql);
        Assert.DoesNotContain("pt_site", sql);
    }

    [Fact]
    public void BuildQuery_Joins_SitePtpDet_On_Domain_Part_And_Selected_Site()
    {
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.Contains("LEFT JOIN qadpro2.dbo.ptp_det AS ptp", sql);
        Assert.Contains("ptp.ptp_domain = @Domain", sql);
        Assert.Contains("ptp.ptp_part = u.ComponentPart", sql);
        Assert.Contains("ptp.ptp_site = @Site", sql);
    }

    [Fact]
    public void BuildQuery_Assigns_Sibling_Rank_Partitioned_By_Parent_Ordered_Component_Reference_Oid()
    {
        // Amendment 2: database collation owns the Component → Reference → OID sibling order via
        // an outer ROW_NUMBER(); the C# traversal consumes the numeric SiblingOrder rank.
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.Contains("ROW_NUMBER() OVER (", sql);
        Assert.Contains("PARTITION BY u.ParentPart", sql);
        Assert.Contains("ORDER BY u.ComponentPart, u.Reference, u.OidPsMstr", sql);
        Assert.Contains("AS SiblingOrder", sql);
    }

    [Fact]
    public void BuildQuery_Uses_Explicit_MaxRecursion_100()
    {
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.Contains("OPTION (MAXRECURSION 100)", sql);
    }

    [Fact]
    public void BuildQuery_Never_Concatenates_Raw_Parent_Values_Into_Sql_Text()
    {
        var maliciousParent = "PARENT'; DROP TABLE ps_mstr; --";
        var (sql, parameters) = QadBomReader.BuildQuery("KTC", "SW", maliciousParent, Effective);

        Assert.DoesNotContain(maliciousParent, sql);
        Assert.Equal(maliciousParent, parameters.Get<string>("ParentPart"));
    }

    [Fact]
    public void BuildQuery_Requires_NonBlank_Parent()
    {
        Assert.Throws<ArgumentNullException>(
            () => QadBomReader.BuildQuery("KTC", "SW", null!, Effective));
        Assert.Throws<ArgumentException>(
            () => QadBomReader.BuildQuery("KTC", "SW", "   ", Effective));
    }

    [Fact]
    public void BuildQuery_Does_Not_Set_Isolation_Level_In_Statement()
    {
        // READ UNCOMMITTED is applied at the connection level by QadConnectionFactory.
        var (sql, _) = QadBomReader.BuildQuery("KTC", "SW", "PARENT-1", Effective);

        Assert.DoesNotContain("SET TRANSACTION", sql, StringComparison.OrdinalIgnoreCase);
    }

    // ===== Normalization (pure C#) =====

    [Theory]
    [InlineData("P", "M", "P")]
    [InlineData("M", "P", "M")]
    [InlineData("C", "P", "C")] // non-P/M site codes pass through unclassified
    [InlineData("N", "M", "N")]
    public void ResolveEffectivePmCode_NonBlank_Site_Value_Wins(string sitePm, string globalPm, string expected)
    {
        Assert.Equal(expected, QadBomReader.ResolveEffectivePmCode(sitePm, globalPm));
    }

    [Fact]
    public void ResolveEffectivePmCode_Null_Site_Falls_Back_To_Global()
    {
        Assert.Equal("M", QadBomReader.ResolveEffectivePmCode(null, "M"));
    }

    [Fact]
    public void ResolveEffectivePmCode_WhitespaceOrEmpty_Site_Falls_Back_To_Global()
    {
        // Live QAD data stores unset P/M codes as empty strings, not NULL — blank handling is
        // load-bearing. A whitespace-only site value is unavailable, not an authoritative value.
        Assert.Equal("P", QadBomReader.ResolveEffectivePmCode("   ", "P"));
        Assert.Equal("M", QadBomReader.ResolveEffectivePmCode("", "M"));
    }

    [Fact]
    public void ResolveEffectivePmCode_Both_Unavailable_Is_Null()
    {
        Assert.Null(QadBomReader.ResolveEffectivePmCode(null, null));
        Assert.Null(QadBomReader.ResolveEffectivePmCode("  ", " "));
    }

    [Fact]
    public void ResolveEffectivePmCode_TrimValues()
    {
        Assert.Equal("P", QadBomReader.ResolveEffectivePmCode("  P  ", null));
        Assert.Equal("M", QadBomReader.ResolveEffectivePmCode(null, " M "));
    }

    [Theory]
    [InlineData("CAP", "ASSY", "CAP ASSY")]
    [InlineData(null, "ASSY", "ASSY")]
    [InlineData("CAP", null, "CAP")]
    public void CombineDescription_Is_Null_Safe(string? d1, string? d2, string? expected)
    {
        // One NULL description segment must not erase the other.
        Assert.Equal(expected, QadBomReader.CombineDescription(d1, d2));
    }

    [Fact]
    public void CombineDescription_All_Blank_Is_Null()
    {
        Assert.Null(QadBomReader.CombineDescription(null, null));
        Assert.Null(QadBomReader.CombineDescription("   ", " \t "));
    }

    [Fact]
    public void CombineDescription_TrimSegments()
    {
        Assert.Equal("CAP ASSY", QadBomReader.CombineDescription("  CAP  ", "  ASSY  "));
    }

    [Fact]
    public void Normalize_Maps_Facts_And_Composes_Pm_Description_Phantom()
    {
        var raw = new QadBomStructuralRawRow(
            OidPsMstr: 100m,
            ParentPart: "A",
            ComponentPart: "B",
            Reference: "010",
            QuantityPer: 4m,
            ScrapPercentage: 2.5m,
            Description1: "CAP",
            Description2: "ASSY",
            SitePmCode: "M",
            GlobalPmCode: "P",
            Phantom: true,
            SiblingOrder: 1);

        var occurrence = QadBomReader.Normalize(raw, level: 2, occurrenceKey: "90/100");

        Assert.Equal("90/100", occurrence.OccurrenceKey);
        Assert.Equal(2, occurrence.Level);
        Assert.Equal("B", occurrence.ComponentPart);
        Assert.Equal("M", occurrence.PmCode);
        Assert.True(occurrence.IsPhantom);
        Assert.Equal("CAP ASSY", occurrence.Description);
        Assert.Equal(4m, occurrence.QuantityPer);
        Assert.Equal(2.5m, occurrence.ScrapPercentage);
    }

    [Fact]
    public void Normalize_Missing_Master_Yields_False_Phantom_Fallback_Pm_And_Null_Descriptions()
    {
        var raw = new QadBomStructuralRawRow(
            OidPsMstr: 100m,
            ParentPart: "A",
            ComponentPart: "B",
            Reference: null,
            QuantityPer: null,
            ScrapPercentage: null,
            Description1: null,
            Description2: null,
            SitePmCode: null,
            GlobalPmCode: "P",
            Phantom: null, // no pt_mstr row
            SiblingOrder: 1);

        var occurrence = QadBomReader.Normalize(raw, level: 1, occurrenceKey: "100");

        Assert.False(occurrence.IsPhantom);
        Assert.Equal("P", occurrence.PmCode);
        Assert.Null(occurrence.Description);
        Assert.Null(occurrence.QuantityPer);
        Assert.Null(occurrence.ScrapPercentage);
    }

    [Fact]
    public void Normalize_Keeps_QtyPer_And_Scrap_Relationship_Level()
    {
        var first = QadBomReader.Normalize(Row(100m, "A", "B", 1, qtyPer: 1m, scrap: 0m), 1, "100");
        var second = QadBomReader.Normalize(Row(200m, "A", "B", 2, qtyPer: 9m, scrap: 5m), 1, "200");

        // Verbatim per relationship row — never multiplied, summed, or extended.
        Assert.Equal(1m, first.QuantityPer);
        Assert.Equal(9m, second.QuantityPer);
        Assert.Equal(0m, first.ScrapPercentage);
        Assert.Equal(5m, second.ScrapPercentage);
    }

    // ===== Traversal (TraverseDepthFirst, synthetic closures) =====

    [Fact]
    public void Traverse_Chain_Traverses_All_Levels_With_Actual_Levels()
    {
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(200m, "B", "C", 1),
            Row(300m, "C", "D", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["B", "C", "D"], result.Select(r => r.ComponentPart).ToArray());
        Assert.Equal([1, 2, 3], result.Select(r => r.Level).ToArray());
        Assert.Equal(["100", "100/200", "100/200/300"], result.Select(r => r.OccurrenceKey).ToArray());
    }

    [Fact]
    public void Traverse_DepthFirst_PreOrder_Completes_Subtree_Before_Next_Sibling()
    {
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(200m, "A", "C", 2),
            Row(300m, "B", "B1", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["B", "B1", "C"], result.Select(r => r.ComponentPart).ToArray());
        Assert.Equal([1, 2, 1], result.Select(r => r.Level).ToArray());
    }

    [Fact]
    public void Traverse_Follows_Sql_SiblingOrder_Not_String_Comparisons_Or_Keys()
    {
        // Amendment 2: SiblingOrder deliberately contradicts both alphabetical component order
        // and OID/key string order — the output must follow the SQL rank exactly, proving C#
        // neither re-derives collation nor orders by the occurrence key.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(900m, "A", "ZZZ", 1),
            Row(100m, "A", "AAA", 2),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["ZZZ", "AAA"], result.Select(r => r.ComponentPart).ToArray());
        Assert.Equal(["900", "100"], result.Select(r => r.OccurrenceKey).ToArray());
    }

    [Fact]
    public void Traverse_Duplicate_Component_Under_Same_Parent_Preserves_Both_Occurrences()
    {
        // Two distinct physical A→B relationships; B's single physical child D is re-listed
        // beneath each B occurrence — nothing is consolidated.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1, reference: "010"),
            Row(200m, "A", "B", 2, reference: "020"),
            Row(300m, "B", "D", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["B", "D", "B", "D"], result.Select(r => r.ComponentPart).ToArray());
        Assert.Equal([1, 2, 1, 2], result.Select(r => r.Level).ToArray());
        Assert.Equal(["100", "100/300", "200", "200/300"], result.Select(r => r.OccurrenceKey).ToArray());
    }

    [Fact]
    public void Traverse_Shared_Physical_Descendant_Via_Two_Paths_Yields_Distinct_Expanded_Occurrences()
    {
        // Amendment 1: D is ONE physical relationship (OID 300) reached through two structural
        // paths (A→B→D and A→C→D). Both expanded occurrences are emitted, at their own levels,
        // with different occurrence keys, and the keys are deterministic across calls.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(200m, "A", "C", 2),
            Row(300m, "B", "D", 1),
            Row(300m, "C", "D", 1),
        ];

        var first = QadBomReader.TraverseDepthFirst("A", rows);
        var second = QadBomReader.TraverseDepthFirst("A", rows);

        var dOccurrences = first.Where(r => r.ComponentPart == "D").ToArray();
        Assert.Equal(2, dOccurrences.Length);
        Assert.Equal(["100/300", "200/300"], dOccurrences.Select(r => r.OccurrenceKey).ToArray());
        Assert.Equal([2, 2], dOccurrences.Select(r => r.Level).ToArray());

        Assert.Equal(
            first.Select(r => r.OccurrenceKey),
            second.Select(r => r.OccurrenceKey));
    }

    [Fact]
    public void Traverse_Same_Component_At_Multiple_Levels_Remains_Separate()
    {
        // D appears at Level 2 (under B) and Level 3 (under F under C) — separate occurrences
        // with their actual levels.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(200m, "A", "C", 2),
            Row(300m, "B", "D", 1),
            Row(400m, "C", "F", 1),
            Row(500m, "F", "D", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["B", "D", "C", "F", "D"], result.Select(r => r.ComponentPart).ToArray());
        Assert.Equal([1, 2, 1, 2, 3], result.Select(r => r.Level).ToArray());
        Assert.Equal(
            ["100", "100/300", "200", "200/400", "200/400/500"],
            result.Select(r => r.OccurrenceKey).ToArray());
    }

    [Fact]
    public void Traverse_Phantom_Intermediate_Is_Retained_And_Descendants_Traversed()
    {
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "PH", 1, phantom: true),
            Row(200m, "PH", "M1", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["PH", "M1"], result.Select(r => r.ComponentPart).ToArray());
        Assert.True(result[0].IsPhantom);
        Assert.Equal(2, result[1].Level);
    }

    [Fact]
    public void Traverse_NonPm_Intermediate_Does_Not_Block_Pm_Descendants()
    {
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "N1", 1, sitePm: "N"),
            Row(200m, "N1", "P1", 1, sitePm: "P"),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["N1", "P1"], result.Select(r => r.ComponentPart).ToArray());
        Assert.Equal("N", result[0].PmCode);
        Assert.Equal("P", result[1].PmCode);
        Assert.Equal(2, result[1].Level);
    }

    [Fact]
    public void Traverse_Level_Is_Preserved_Through_Hidden_Intermediate()
    {
        // A hidden (non-P/M) Level 1 intermediate keeps its descendant at its actual Level 2 —
        // no cosmetic renumbering.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "HIDDEN", 1, sitePm: "2"),
            Row(200m, "HIDDEN", "VISIBLE", 1, sitePm: "M"),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(1, result[0].Level);
        Assert.Equal(2, result[1].Level);
    }

    [Fact]
    public void Traverse_OccurrenceKey_Uses_Exact_Decimal_Oid_Invariant_Format()
    {
        // Live-confirmed OIDs are decimal(28,10) with fractional parts; the key must serialize
        // the exact value, invariant-culture.
        var oid = 201306300024529805.0009000000m;
        IReadOnlyList<QadBomStructuralRawRow> rows = [Row(oid, "A", "B", 1)];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal("201306300024529805.0009000000", result[0].OccurrenceKey);
    }

    [Fact]
    public void Traverse_Empty_Closure_Returns_Empty()
    {
        Assert.Empty(QadBomReader.TraverseDepthFirst("A", []));
    }

    [Fact]
    public void Traverse_Root_Matching_Is_Case_Insensitive()
    {
        IReadOnlyList<QadBomStructuralRawRow> rows = [Row(100m, "ABC", "B", 1)];

        var result = QadBomReader.TraverseDepthFirst("abc", rows);

        Assert.Single(result);
        Assert.Equal("B", result[0].ComponentPart);
    }

    [Fact]
    public void Traverse_Cycle_Fails_With_Descriptive_Exception()
    {
        // A→B→A: the walker must fail, not loop. (Real cyclical BOMs fail first at the SQL
        // MAXRECURSION ceiling; this guard keeps the pure walker safe against any row set.)
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(200m, "B", "A", 1),
        ];

        var ex = Assert.Throws<InvalidOperationException>(() => QadBomReader.TraverseDepthFirst("A", rows));

        Assert.Contains("cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Traverse_Diamond_Is_Not_A_Cycle()
    {
        // Same part (D) beneath two different parents is legitimate structure — no exception,
        // both occurrences preserved.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(200m, "A", "C", 2),
            Row(300m, "B", "D", 1),
            Row(400m, "C", "D", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["B", "D", "C", "D"], result.Select(r => r.ComponentPart).ToArray());
    }

    [Fact]
    public void Traverse_Unreachable_Relationship_Is_Not_Emitted()
    {
        // The closure is root-scoped: a relationship whose parent is never reached from the root
        // is not part of this BOM.
        IReadOnlyList<QadBomStructuralRawRow> rows =
        [
            Row(100m, "A", "B", 1),
            Row(900m, "X", "Y", 1),
        ];

        var result = QadBomReader.TraverseDepthFirst("A", rows);

        Assert.Equal(["B"], result.Select(r => r.ComponentPart).ToArray());
    }

    [Fact]
    public void Traverse_Requires_NonBlank_Root()
    {
        IReadOnlyList<QadBomStructuralRawRow> empty = [];

        Assert.Throws<ArgumentNullException>(() => QadBomReader.TraverseDepthFirst(null!, empty));
        Assert.Throws<ArgumentException>(() => QadBomReader.TraverseDepthFirst("   ", empty));
    }

    // ===== Helpers =====

    private static QadBomStructuralRawRow Row(
        decimal oid,
        string parent,
        string component,
        long siblingOrder,
        string? reference = null,
        decimal? qtyPer = 1m,
        decimal? scrap = null,
        string? desc1 = null,
        string? desc2 = null,
        string? sitePm = null,
        string? globalPm = null,
        bool? phantom = null) => new(
        OidPsMstr: oid,
        ParentPart: parent,
        ComponentPart: component,
        Reference: reference,
        QuantityPer: qtyPer,
        ScrapPercentage: scrap,
        Description1: desc1,
        Description2: desc2,
        SitePmCode: sitePm,
        GlobalPmCode: globalPm,
        Phantom: phantom,
        SiblingOrder: siblingOrder);

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
