using Kst.Application.ComponentOrders;
using Kst.Integrations.Shortages.ComponentOrders;
using Kst.Integrations.Shortages.Options;
using Microsoft.Data.SqlClient;

namespace Kst.Integrations.Qad.Tests.Shortages;

public sealed class ShortagesComponentOrderEnrichmentReaderTests
{
    [Fact]
    public void Supplier_Query_Uses_Exact_Parameterized_Name_Scope_And_Deterministic_Ranking()
    {
        using var connection = new SqlConnection();
        using var command = ShortagesComponentOrderEnrichmentReader.BuildSupplierCommand(connection, [" 001 ", "001", "A-1"]);

        Assert.Contains("ps.[Supplier Name]", command.CommandText);
        Assert.Contains("ROW_NUMBER() OVER (PARTITION BY ps.[Supplier Name] ORDER BY ps.[Date] DESC, ps.ID DESC)", command.CommandText);
        Assert.Contains("WHERE RowNumber = 1", command.CommandText);
        Assert.DoesNotContain("[Supplier Nbr]", command.CommandText);
        Assert.DoesNotContain("LTRIM", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CAST(", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(" 001 ", command.Parameters["@Supplier0"].Value);
        Assert.Equal("001", command.Parameters["@Supplier1"].Value);
        Assert.Equal("A-1", command.Parameters["@Supplier2"].Value);
    }

    [Fact]
    public void Comment_Query_Uses_Exact_Site_Component_Active_Predicate_And_Deterministic_Ranking()
    {
        using var connection = new SqlConnection();
        using var command = ShortagesComponentOrderEnrichmentReader.BuildCommentCommand(connection, "SW", [" C1 ", "C1"]);

        Assert.Contains("sm.Site = @Site", command.CommandText);
        Assert.Contains("sm.isRemoved = 0", command.CommandText);
        Assert.Contains("ORDER BY sm.[Modification Date] DESC, sm.[Added Record Date] DESC, sm.id DESC", command.CommandText);
        Assert.Contains("sm.[Current Comments] AS CurrentComments", command.CommandText);
        Assert.Equal("SW", command.Parameters["@Site"].Value);
        Assert.Equal(" C1 ", command.Parameters["@Component0"].Value);
        Assert.Equal("C1", command.Parameters["@Component1"].Value);
    }

    [Fact]
    public async Task Empty_Scopes_Return_Empty_Without_Loading_Configuration_Or_Opening_A_Connection()
    {
        var reader = new ShortagesComponentOrderEnrichmentReader(new ShortagesSecretFileLoader("missing-secret.json"));

        var result = await reader.ReadAsync(new ComponentOrderEnrichmentRequest("SW", [], []));

        Assert.Empty(result.CommentsByComponent);
        Assert.Empty(result.RisksBySupplier);
    }

    [Fact]
    public async Task Missing_Configuration_Propagates_Instead_Of_Returning_Blank_Enrichment()
    {
        var reader = new ShortagesComponentOrderEnrichmentReader(new ShortagesSecretFileLoader("missing-secret.json"));

        var action = () => reader.ReadAsync(new ComponentOrderEnrichmentRequest("SW", ["C1"], [])).GetAwaiter().GetResult();

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public async Task Cancellation_Propagates_Before_Configuration_Or_Connection_Use()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var reader = new ShortagesComponentOrderEnrichmentReader(new ShortagesSecretFileLoader("missing-secret.json"));

        var action = () => reader.ReadAsync(new ComponentOrderEnrichmentRequest("SW", ["C1"], []), cancellation.Token).GetAwaiter().GetResult();

        Assert.Throws<OperationCanceledException>(action);
    }

    [Fact]
    public void Empty_Query_Scopes_Are_Rejected_Before_Sql_Is_Built()
    {
        using var connection = new SqlConnection();

        Assert.Throws<ArgumentException>(() => ShortagesComponentOrderEnrichmentReader.BuildSupplierCommand(connection, []));
        Assert.Throws<ArgumentException>(() => ShortagesComponentOrderEnrichmentReader.BuildCommentCommand(connection, "SW", []));
    }

    [Fact]
    public void Mapping_Preserves_Bit_Values_And_Nullable_Multiline_Comment_Text()
    {
        var risk = ShortagesComponentOrderEnrichmentReader.MapRisk(true, false);
        var multiline = "first line\r\nsecond line";

        Assert.True(risk.IsCreditHold);
        Assert.False(risk.IsCia);
        Assert.Null(ShortagesComponentOrderEnrichmentReader.NormalizeComment(null));
        Assert.Equal(multiline, ShortagesComponentOrderEnrichmentReader.NormalizeComment(multiline));
    }
}
