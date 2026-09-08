namespace Kst.Application.Shortages;

public interface IIssueDaysReader
{
    Task<int?> ReadAsync(string site, CancellationToken cancellationToken = default);
}
