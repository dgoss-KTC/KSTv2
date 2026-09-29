using Kst.Domain.LongTermShortages;

namespace Kst.Domain.Tests.LongTermShortages;

public sealed class LongTermShortagePopulationOptionsTests
{
    [Fact]
    public void Default_ExcludesManufacturedPhantomAndBothClassifiedComponents_ButIncludesNormal()
    {
        var options = LongTermShortagePopulationOptions.Default;

        Assert.True(options.IsIncluded(isManufactured: false, isPhantom: false));
        Assert.False(options.IsIncluded(isManufactured: true, isPhantom: false));
        Assert.False(options.IsIncluded(isManufactured: false, isPhantom: true));
        Assert.False(options.IsIncluded(isManufactured: true, isPhantom: true));
    }

    [Fact]
    public void IncludeManufacturedParts_IncludesOnlyTheManufacturedClass()
    {
        var options = new LongTermShortagePopulationOptions(IncludeManufacturedParts: true);

        Assert.True(options.IsIncluded(isManufactured: false, isPhantom: false));
        Assert.True(options.IsIncluded(isManufactured: true, isPhantom: false));
        Assert.False(options.IsIncluded(isManufactured: false, isPhantom: true));
        // A component that is both manufactured and phantom still requires the phantom option.
        Assert.False(options.IsIncluded(isManufactured: true, isPhantom: true));
    }

    [Fact]
    public void IncludePhantoms_IncludesOnlyThePhantomClass()
    {
        var options = new LongTermShortagePopulationOptions(IncludePhantoms: true);

        Assert.True(options.IsIncluded(isManufactured: false, isPhantom: false));
        Assert.False(options.IsIncluded(isManufactured: true, isPhantom: false));
        Assert.True(options.IsIncluded(isManufactured: false, isPhantom: true));
        // A component that is both manufactured and phantom still requires the manufactured option.
        Assert.False(options.IsIncluded(isManufactured: true, isPhantom: true));
    }

    [Fact]
    public void BothOptionsEnabled_IncludesEveryClassification()
    {
        var options = new LongTermShortagePopulationOptions(IncludeManufacturedParts: true, IncludePhantoms: true);

        Assert.True(options.IsIncluded(isManufactured: false, isPhantom: false));
        Assert.True(options.IsIncluded(isManufactured: true, isPhantom: false));
        Assert.True(options.IsIncluded(isManufactured: false, isPhantom: true));
        Assert.True(options.IsIncluded(isManufactured: true, isPhantom: true));
    }

    [Fact]
    public void DistinctOptionValues_AreDistinctCacheIdentities()
    {
        Assert.NotEqual(LongTermShortagePopulationOptions.Default, new LongTermShortagePopulationOptions(IncludeManufacturedParts: true));
        Assert.NotEqual(LongTermShortagePopulationOptions.Default, new LongTermShortagePopulationOptions(IncludePhantoms: true));
        Assert.NotEqual(new LongTermShortagePopulationOptions(IncludeManufacturedParts: true), new LongTermShortagePopulationOptions(IncludePhantoms: true));
        Assert.Equal(new LongTermShortagePopulationOptions(true, true), new LongTermShortagePopulationOptions(IncludeManufacturedParts: true, IncludePhantoms: true));
    }
}
