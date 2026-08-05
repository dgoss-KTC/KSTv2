using Kst.Domain.Workspaces;

namespace Kst.Domain.Tests.Workspaces;

public sealed class ParentPartNormalizerTests
{
    [Fact]
    public void Normalize_Trims_Whitespace()
    {
        var result = ParentPartNormalizer.Normalize(["  ABC100  "]);
        Assert.Equal(["ABC100"], result);
    }

    [Fact]
    public void Normalize_Removes_Blank_Entries()
    {
        var result = ParentPartNormalizer.Normalize(["ABC100", "", "   ", null]);
        Assert.Equal(["ABC100"], result);
    }

    [Fact]
    public void Normalize_Deduplicates_Case_Sensitively()
    {
        var result = ParentPartNormalizer.Normalize(["ABC100", "ABC100", " ABC100 "]);
        Assert.Equal(["ABC100"], result);
    }

    [Fact]
    public void Normalize_Preserves_Distinct_Casing_As_Separate_Entries()
    {
        var result = ParentPartNormalizer.Normalize(["ABC100", "abc100"]);
        Assert.Equal(["ABC100", "abc100"], result);
    }

    [Fact]
    public void Normalize_Preserves_First_Occurrence_Order()
    {
        var result = ParentPartNormalizer.Normalize(["C", "A", "B", "A"]);
        Assert.Equal(["C", "A", "B"], result);
    }

    [Fact]
    public void Normalize_Null_Input_Returns_Empty_List()
    {
        var result = ParentPartNormalizer.Normalize(null);
        Assert.Empty(result);
    }

    [Fact]
    public void Normalize_Empty_Input_Returns_Empty_List()
    {
        var result = ParentPartNormalizer.Normalize([]);
        Assert.Empty(result);
    }

    [Fact]
    public void SetEquals_Same_Elements_Different_Order_Are_Equal()
    {
        Assert.True(ParentPartNormalizer.SetEquals(["A", "B", "C"], ["C", "A", "B"]));
    }

    [Fact]
    public void SetEquals_Different_Elements_Are_Not_Equal()
    {
        Assert.False(ParentPartNormalizer.SetEquals(["A", "B"], ["A", "C"]));
    }

    [Fact]
    public void SetEquals_Different_Counts_Are_Not_Equal()
    {
        Assert.False(ParentPartNormalizer.SetEquals(["A", "B"], ["A", "B", "C"]));
    }

    [Fact]
    public void SetEquals_Both_Empty_Are_Equal()
    {
        Assert.True(ParentPartNormalizer.SetEquals([], []));
    }

    [Fact]
    public void SetEquals_Is_Case_Sensitive()
    {
        Assert.False(ParentPartNormalizer.SetEquals(["ABC100"], ["abc100"]));
    }
}
