using Kst.Integrations.Qad.Mps;

namespace Kst.Integrations.Qad.Tests.Mps;

public sealed class QadSiteDomainMapTests
{
    [Theory]
    [InlineData("NW", "KTC")]
    [InlineData("SW", "KTC")]
    [InlineData("AR", "KTC")]
    [InlineData("MN", "KTC")]
    [InlineData("MS", "KTC")]
    [InlineData("KV", "KTV")]
    [InlineData("kv", "KTV")]
    public void Resolve_Maps_Known_Sites(string site, string expectedDomain)
    {
        Assert.Equal(expectedDomain, QadSiteDomainMap.Resolve(site));
    }

    [Fact]
    public void Resolve_Throws_For_Unknown_Site()
    {
        Assert.Throws<InvalidOperationException>(() => QadSiteDomainMap.Resolve("ZZ"));
    }

    [Fact]
    public void TryResolve_Returns_False_For_Unknown_Site()
    {
        var found = QadSiteDomainMap.TryResolve("ZZ", out var domain);
        Assert.False(found);
        Assert.Equal(string.Empty, domain);
    }
}
