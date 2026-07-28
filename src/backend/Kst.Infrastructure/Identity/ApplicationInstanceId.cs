namespace Kst.Infrastructure.Identity;

/// <summary>
/// Provides a stable instance identifier for this process lifetime.
/// Generated once at startup and reused across all requests.
/// </summary>
public static class ApplicationInstanceId
{
    private static readonly string _value = Guid.NewGuid().ToString("D");

    public static string Value => _value;
}
