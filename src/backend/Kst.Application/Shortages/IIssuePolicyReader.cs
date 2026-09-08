namespace Kst.Application.Shortages;

public interface IIssuePolicyReader
{
    Task<bool> ReadAsync(string site, string partNumber, CancellationToken cancellationToken = default);
}
