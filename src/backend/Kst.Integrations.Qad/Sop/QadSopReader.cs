using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Kst.Domain.Sop;
using Kst.Integrations.Qad.Mps;
using Kst.Integrations.Qad.Options;

namespace Kst.Integrations.Qad.Sop;

/// <summary>Owner-admitted fixed procedure only. Its unavailable internal behavior is not asserted read-only.</summary>
public sealed class QadSopReader(QadConnectionOptions options)
{
    public const string Procedure = "dbo.sp_QAD_ktmpswkm";
    private static readonly string[] FixedHeaders = ["Customer", "Part", "St", "Planr", "Prod", "type", "Time Std", " Falldown"];

    public static SqlCommand BuildCommand(SqlConnection connection, string site, SopRange range, int timeoutSeconds)
    {
        if (!SopRules.ValidRequest(site, "planr", [range])) throw new ArgumentException("Invalid S&OP scope.");
        var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = Procedure;
        command.CommandTimeout = Math.Clamp(timeoutSeconds, 1, 60);
        command.Parameters.AddWithValue("@Domain", QadSiteDomainMap.Resolve(site));
        command.Parameters.AddWithValue("@Site", site);
        command.Parameters.AddWithValue("@pl1", range.From);
        command.Parameters.AddWithValue("@pl2", range.To);
        command.Parameters.AddWithValue("@hours_units", 0);
        return command;
    }

    public async Task<SopSource> ReadAsync(string site, IReadOnlyList<SopRange> ranges, CancellationToken ct)
    {
        if (!options.IsConfigured) throw new InvalidOperationException("QAD is not configured.");
        if (!SopRules.ValidRequest(site, "planr", ranges)) throw new ArgumentException("Invalid S&OP scope.");
        // Bound the complete multi-pair acquisition as well as each command. No partial success.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(100));
        var token = deadline.Token;
        await using var connection = await QadConnectionFactory.OpenAsync(options, token);
        return await ReadPairsAsync(ranges, async (range, cancel) =>
        {
            await using var command = BuildCommand(connection, site, range, options.CommandTimeoutSeconds);
            await using var reader = await command.ExecuteReaderAsync(cancel);
            return await ParseAsync(reader, cancel);
        }, token);
    }

    public static async Task<SopSource> ReadPairsAsync(IReadOnlyList<SopRange> ranges,
        Func<SopRange, CancellationToken, Task<SopSource>> read, CancellationToken ct)
    {
        SopSource? first = null;
        var rows = new List<SopRow>();
        long bytes = 0;
        foreach (var range in ranges)
        {
            ct.ThrowIfCancellationRequested();
            var source = await read(range, ct);
            ct.ThrowIfCancellationRequested();
            if (first is not null && (!first.Headers.SequenceEqual(source.Headers) || !first.WeekLabels.SequenceEqual(source.WeekLabels)))
                throw SchemaError();
            first ??= source;
            foreach (var row in source.Rows)
            {
                bytes += row.Fixed.Concat(row.Weeks).Sum(c => c.Text is not null ? (long)c.Text.Length * 2 + 16 : 16);
                if (rows.Count >= SopRules.MaxRows || (long)(rows.Count + 1) * (8 + source.WeekLabels.Count) > SopRules.MaxCells
                    || bytes > SopRules.MaxSourceBytes) throw new InvalidOperationException("S&OP result exceeds report capacity.");
                rows.Add(row);
            }
        }
        return first is null ? throw SchemaError() : new(first.Headers, first.WeekLabels, rows);
    }

    public static async Task<SopSource> ParseAsync(DbDataReader reader, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (reader.FieldCount is < 10 or > 9 + SopRules.MaxWeeks) throw SchemaError();
        var headers = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        if (!headers.Take(8).SequenceEqual(FixedHeaders, StringComparer.Ordinal)) throw SchemaError();
        // Raw I is removed without interpreting its values. All date identities must be unambiguous,
        // consecutive Sundays; only the remaining identities are displayed as Mondays.
        var rawWeeks = headers.Skip(8).Select(ParseWeekHeader).ToArray();
        for (var i = 1; i < rawWeeks.Length; i++)
            if (rawWeeks[i] != rawWeeks[i - 1].AddDays(7)) throw SchemaError();
        var weeks = rawWeeks.Skip(1).Select(d => d.AddDays(1)).ToArray();
        var rows = new List<SopRow>();
        long bytes = 0;
        while (await reader.ReadAsync(ct))
        {
            if (rows.Count >= SopRules.MaxRows || (long)(rows.Count + 1) * (8 + weeks.Length) > SopRules.MaxCells) throw new InvalidOperationException("S&OP result exceeds report capacity.");
            var fixedCells = Enumerable.Range(0, 8).Select(i => Cell(reader.GetValue(i))).ToArray();
            var weekly = Enumerable.Range(9, reader.FieldCount - 9).Select(i => Cell(reader.GetValue(i))).ToArray();
            bytes += fixedCells.Concat(weekly).Sum(c => c.Text is not null ? (long)c.Text.Length * 2 + 16 : 16);
            if (bytes > SopRules.MaxSourceBytes) throw new InvalidOperationException("S&OP result exceeds report capacity.");
            rows.Add(new(fixedCells, weekly));
        }
        if (await reader.NextResultAsync(ct)) throw SchemaError();
        return new(headers.Take(8).ToArray(), weeks, rows);
    }

    public static DateOnly ParseWeekHeader(string header)
    {
        // Invariant ISO date-only or evidenced m/d/yyyy display format; no culture guesses or two-digit years.
        if (!DateOnly.TryParseExact(header, ["yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date) || date.DayOfWeek != DayOfWeek.Sunday || date.Year is < 1900 or > 2200)
            throw SchemaError();
        return date;
    }

    private static SopCell Cell(object value) => value switch
    {
        DBNull => SopCell.Blank,
        string text when text.Length <= SopRules.MaxText => new(Text: text),
        byte or short or int or long or decimal or float or double => Numeric(value),
        _ => throw SchemaError()
    };
    private static SopCell Numeric(object value)
    {
        var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return double.IsFinite(number) ? new(Number: number) : throw SchemaError();
    }
    private static InvalidOperationException SchemaError() => new("S&OP procedure output has an unsupported schema or value. No workbook was prepared.");
}
