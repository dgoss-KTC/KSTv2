using System.Globalization;
using Kst.Domain.Mps;

namespace Kst.Domain.PlanningWorkbook;

/// <summary>
/// Adjustment issues in reporting precedence order (the first present issue is the request's
/// primary issue). Negative projected inventory, SO/MPS mismatches, released or firm work orders,
/// and lead time are deliberately not issues; there is no frozen fence.
/// </summary>
public enum MpsAdjustmentIssue
{
    SnapshotChanged,
    NotAdjustable,
    InvalidNumber,
    NotWholeNumber,
    NotAWeek,
    WeekPassed,
    PartOutOfScope,
    DuplicateIdentity,
    NegativeResult
}

public static class MpsAdjustmentIssueCodes
{
    public static string ToCode(this MpsAdjustmentIssue issue) => issue switch
    {
        MpsAdjustmentIssue.SnapshotChanged => "snapshot-changed",
        MpsAdjustmentIssue.NotAdjustable => "not-adjustable",
        MpsAdjustmentIssue.InvalidNumber => "invalid-number",
        MpsAdjustmentIssue.NotWholeNumber => "not-whole-number",
        MpsAdjustmentIssue.NotAWeek => "not-a-week",
        MpsAdjustmentIssue.WeekPassed => "week-passed",
        MpsAdjustmentIssue.PartOutOfScope => "part-out-of-scope",
        MpsAdjustmentIssue.DuplicateIdentity => "duplicate-identity",
        MpsAdjustmentIssue.NegativeResult => "negative-result",
        _ => throw new ArgumentOutOfRangeException(nameof(issue))
    };
}

public enum MpsAdjustmentInputKind
{
    Value,
    NoOp,
    InvalidNumber,
    NotWholeNumber
}

public sealed record MpsAdjustmentInputResult(MpsAdjustmentInputKind Kind, decimal? Value);

/// <summary>
/// Signed whole-number adjustment input. Accepted text is an optional leading <c>-</c> followed by
/// ASCII digits. Decimal-point forms (including <c>10.0</c>) are not whole numbers; <c>+</c>,
/// exponents, grouping separators, whitespace and other text are invalid. Blank, <c>0</c> and
/// <c>-0</c> are no-ops (not proposals). Only the input is constrained; source quantities and prices
/// are never rounded.
/// </summary>
public static class MpsAdjustmentInput
{
    public static MpsAdjustmentInputResult Parse(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return new(MpsAdjustmentInputKind.NoOp, null);

        var digits = text.StartsWith('-') ? text.AsSpan(1) : text.AsSpan();
        var point = digits.IndexOf('.');
        if (point > 0 && point < digits.Length - 1 && AllDigits(digits[..point]) && AllDigits(digits[(point + 1)..]))
            return new(MpsAdjustmentInputKind.NotWholeNumber, null);
        if (digits.Length == 0 || !AllDigits(digits))
            return new(MpsAdjustmentInputKind.InvalidNumber, null);
        if (!decimal.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
            return new(MpsAdjustmentInputKind.InvalidNumber, null);
        return value == 0m
            ? new(MpsAdjustmentInputKind.NoOp, null)
            : new(MpsAdjustmentInputKind.Value, value);
    }

    private static bool AllDigits(ReadOnlySpan<char> span)
    {
        foreach (var c in span)
        {
            if (c is < '0' or > '9')
                return false;
        }
        return true;
    }
}

/// <summary>One requested adjustment. A null <see cref="WeekLabel"/> targets Falldown.</summary>
public sealed record MpsAdjustmentEntry(string Part, DateOnly? WeekLabel, string? AdjustmentText);

public sealed record MpsAdjustmentRequest(Guid MpsSnapshotId, IReadOnlyList<MpsAdjustmentEntry> Entries);

/// <summary>The loaded MPS snapshot the request is validated against. No QAD access is involved.</summary>
public sealed record MpsAdjustmentContext(
    Guid LoadedMpsSnapshotId,
    IReadOnlyList<MpsResolvedPart> ResolvedParts,
    IReadOnlyList<MpsSourceRow> MpsRows,
    DateOnly Today);

public sealed record MpsAdjustmentEntryResult(
    int Index,
    MpsAdjustmentEntry Entry,
    MpsAdjustmentIssue? Issue,
    bool IsNoOp,
    decimal? Adjustment,
    decimal? ExistingMps,
    decimal? ProposedMps);

/// <summary>One QAD Mass Updater output row: part, signed adjustment, Monday week label.</summary>
public sealed record MpsMassUpdateRow(string Part, decimal Adjustment, DateOnly WeekLabel);

public sealed record MpsAdjustmentValidationResult(
    IReadOnlyList<MpsAdjustmentEntryResult> Entries,
    MpsAdjustmentIssue? Issue,
    int AffectedRowCount,
    IReadOnlyList<MpsMassUpdateRow> Rows)
{
    public bool IsValid => Issue is null;
}

/// <summary>
/// Validates a whole adjustment request against the loaded MPS snapshot and derives Mass Updater
/// rows. Any issue rejects the entire request (no rows). Baselines are the Due-Date business-week
/// MPS quantity of the retained snapshot rows (accepted <see cref="MpsScheduleBuilder"/>
/// projection), independent of any visible horizon. Valid rows are ordered by retained part order,
/// then week. Only a negative resulting MPS quantity is a quantity rule; zero is valid.
/// </summary>
public static class MpsAdjustmentValidator
{
    public static MpsAdjustmentValidationResult Validate(MpsAdjustmentRequest request, MpsAdjustmentContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.MpsSnapshotId != context.LoadedMpsSnapshotId)
        {
            var rejected = request.Entries
                .Select((e, i) => new MpsAdjustmentEntryResult(i, e, MpsAdjustmentIssue.SnapshotChanged, false, null, null, null))
                .ToList();
            return new(rejected, MpsAdjustmentIssue.SnapshotChanged, rejected.Count, []);
        }

        var results = Evaluate(request.Entries, context);
        var issues = results.Where(r => r.Issue is not null).ToList();
        if (issues.Count > 0)
            return new(results, issues.Min(r => r.Issue), issues.Count, []);

        var partOrder = context.ResolvedParts
            .Select((p, i) => (p.ParentPart, i))
            .ToDictionary(x => x.ParentPart, x => x.i, StringComparer.OrdinalIgnoreCase);
        var rows = results
            .Where(r => !r.IsNoOp)
            .Select(r => new MpsMassUpdateRow(
                context.ResolvedParts[partOrder[r.Entry.Part]].ParentPart, r.Adjustment!.Value, r.Entry.WeekLabel!.Value))
            .OrderBy(r => partOrder[r.Part])
            .ThenBy(r => r.WeekLabel)
            .ToList();
        return new(results, null, 0, rows);
    }

    /// <summary>Per-entry evaluation shared with draft reconciliation; does not check snapshot identity.</summary>
    internal static IReadOnlyList<MpsAdjustmentEntryResult> Evaluate(
        IReadOnlyList<MpsAdjustmentEntry> entries, MpsAdjustmentContext context)
    {
        var currentWeekStart = MpsBusinessCalendar.GetBusinessWeekStart(context.Today);
        var resolved = context.ResolvedParts.Select(p => p.ParentPart).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var staged = new List<(MpsAdjustmentEntry Entry, MpsAdjustmentIssue? Issue, bool NoOp, decimal? Value)>(entries.Count);
        foreach (var entry in entries)
        {
            var input = MpsAdjustmentInput.Parse(entry.AdjustmentText);
            MpsAdjustmentIssue? issue = entry.WeekLabel switch
            {
                null => MpsAdjustmentIssue.NotAdjustable,
                _ when input.Kind == MpsAdjustmentInputKind.InvalidNumber => MpsAdjustmentIssue.InvalidNumber,
                _ when input.Kind == MpsAdjustmentInputKind.NotWholeNumber => MpsAdjustmentIssue.NotWholeNumber,
                { } w when w.DayOfWeek != DayOfWeek.Monday => MpsAdjustmentIssue.NotAWeek,
                { } w when MpsBusinessCalendar.GetBusinessWeekStart(w) < currentWeekStart => MpsAdjustmentIssue.WeekPassed,
                _ when !resolved.Contains(entry.Part) => MpsAdjustmentIssue.PartOutOfScope,
                _ => null
            };
            staged.Add((entry, issue, input.Kind == MpsAdjustmentInputKind.NoOp, input.Value));
        }

        // Identity is structural: any repeated part/week (including no-op entries) is a duplicate.
        var identityCounts = staged
            .Where(s => s.Entry.WeekLabel is not null)
            .GroupBy(s => (s.Entry.Part, s.Entry.WeekLabel!.Value), PartWeekComparer.Instance)
            .ToDictionary(g => g.Key, g => g.Count(), PartWeekComparer.Instance);

        var baselines = Baselines(staged.Where(s => s.Issue is null).Select(s => s.Entry), context, currentWeekStart);

        var results = new List<MpsAdjustmentEntryResult>(staged.Count);
        for (var i = 0; i < staged.Count; i++)
        {
            var (entry, issue, noOp, value) = staged[i];
            if (issue is null && identityCounts[(entry.Part, entry.WeekLabel!.Value)] > 1)
                issue = MpsAdjustmentIssue.DuplicateIdentity;

            decimal? existing = null;
            decimal? proposed = null;
            if (issue is null)
            {
                existing = baselines[(entry.Part, entry.WeekLabel!.Value)];
                proposed = existing + (value ?? 0m);
                if (!noOp && proposed < 0m)
                    issue = MpsAdjustmentIssue.NegativeResult;
            }

            results.Add(new(i, entry, issue, noOp && issue is null, value, existing, proposed));
        }
        return results;
    }

    private static Dictionary<(string Part, DateOnly Week), decimal> Baselines(
        IEnumerable<MpsAdjustmentEntry> entries, MpsAdjustmentContext context, DateOnly currentWeekStart)
    {
        var keys = entries.Select(e => (e.Part, Week: e.WeekLabel!.Value)).ToList();
        var result = new Dictionary<(string Part, DateOnly Week), decimal>(PartWeekComparer.Instance);
        if (keys.Count == 0)
            return result;

        var horizon = keys.Max(k => MpsBusinessCalendar.GetBusinessWeekStart(k.Week).DayNumber - currentWeekStart.DayNumber) / 7 + 1;
        var schedules = MpsScheduleBuilder.Build(
            context.ResolvedParts, context.MpsRows, MpsDateBasis.DueDate, horizon, context.Today);
        foreach (var schedule in schedules)
        {
            foreach (var bucket in schedule.Buckets.Where(b => b.Kind == MpsBucketKind.Weekly))
                result.TryAdd((schedule.ParentPart, bucket.WeekLabel!.Value), bucket.Quantity);
        }
        return result;
    }
}
