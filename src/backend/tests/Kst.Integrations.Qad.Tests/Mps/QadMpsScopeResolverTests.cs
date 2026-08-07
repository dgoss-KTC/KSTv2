using Kst.Integrations.Qad.Mps;

namespace Kst.Integrations.Qad.Tests.Mps;

public sealed class QadMpsScopeResolverTests
{
    [Fact]
    public void BuildDescriptionLookupQuery_Adds_One_Parameter_Per_Part_Plus_Domain()
    {
        var (sql, parameters) = QadMpsScopeResolver.BuildDescriptionLookupQuery("KTC", ["ABC100", "ABC200"]);

        Assert.Equal("KTC", parameters.Get<string>("Domain"));
        Assert.Equal("ABC100", parameters.Get<string>("Part0"));
        Assert.Equal("ABC200", parameters.Get<string>("Part1"));
        Assert.Contains("@Part0", sql);
        Assert.Contains("@Part1", sql);
    }

    [Fact]
    public void BuildDescriptionLookupQuery_Uses_LeftJoin_So_Missing_ItemMaster_Rows_Still_Return_The_Part()
    {
        var (sql, _) = QadMpsScopeResolver.BuildDescriptionLookupQuery("KTC", ["ABC100"]);

        Assert.Contains("LEFT JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope.ParentPart AS ParentPart", sql);
    }

    [Fact]
    public void BuildDescriptionLookupQuery_Never_Concatenates_Raw_Part_Values_Into_Sql_Text()
    {
        var maliciousPart = "ABC'; DROP TABLE pt_mstr; --";
        var (sql, parameters) = QadMpsScopeResolver.BuildDescriptionLookupQuery("KTC", [maliciousPart]);

        Assert.DoesNotContain(maliciousPart, sql);
        Assert.Equal(maliciousPart, parameters.Get<string>("Part0"));
    }
}
