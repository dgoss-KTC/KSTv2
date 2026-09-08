namespace Kst.Application.Shortages;

/// <summary>Reads whether a part has an effective QAD supplier-scheduled KSS relationship independent of conventional PO availability.</summary>
public interface IKssScheduleReader
{
    Task<bool> IsKssAsync(string site, string partNumber, DateOnly today, CancellationToken cancellationToken = default);
}
