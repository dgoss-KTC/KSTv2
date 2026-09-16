using Kst.Domain.LongTermShortages;

namespace Kst.Domain.Tests.LongTermShortages;

public sealed class LongTermShortageQuantityDisplayPolicyTests
{
    [Theory]
    [InlineData("EA", 4.5, 5)]
    [InlineData(" each ", -4.5, -5)]
    [InlineData("ML", 73.44360902, 73.4436)]
    [InlineData("ML", -73.44360902, -73.4437)]
    public void Round_AppliesApprovedUomSpecificDisplayPolicy(string unitOfMeasure, double raw, double expected)
    {
        Assert.Equal((decimal)expected, LongTermShortageQuantityDisplayPolicy.Round((decimal)raw, unitOfMeasure));
    }
}
