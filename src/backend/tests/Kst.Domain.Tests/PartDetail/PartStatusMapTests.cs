using Kst.Domain.PartDetail;

namespace Kst.Domain.Tests.PartDetail;

public sealed class PartStatusMapTests
{
    [Theory]
    [InlineData("A", "AEMR")]
    [InlineData("B", "BYPASS")]
    [InlineData("C", "CURRENT")]
    [InlineData("E", "END OF LIFE")]
    [InlineData("F", "FORECAST")]
    [InlineData("H", "PURCHASING HOLD")]
    [InlineData("I", "INACTIVE PURCHASED PARTS")]
    [InlineData("M", "MFA")]
    [InlineData("N", "NPI")]
    [InlineData("O", "OBSOLETE")]
    [InlineData("P", "PROTO")]
    [InlineData("Q", "QUOTED PARTS")]
    [InlineData("U", "UNRELEASED")]
    public void Describe_Maps_Known_Codes(string code, string expectedDescription)
    {
        Assert.Equal(expectedDescription, PartStatusMap.Describe(code));
    }

    [Fact]
    public void Describe_Is_Case_Insensitive()
    {
        Assert.Equal("CURRENT", PartStatusMap.Describe("c"));
    }

    [Fact]
    public void Describe_Returns_Null_For_Unknown_Code()
    {
        Assert.Null(PartStatusMap.Describe("Z"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Describe_Returns_Null_For_Blank_Code(string? code)
    {
        Assert.Null(PartStatusMap.Describe(code));
    }
}
