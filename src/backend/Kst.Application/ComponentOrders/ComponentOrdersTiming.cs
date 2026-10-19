using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kst.Application.ComponentOrders;

/// <summary>
/// Client-observed Component Orders phase timings. Children overlap their enclosing phases;
/// parallel BOM children overlap one another. TraceId correlates the request, not source values.
/// No exception payload or request/source contents are logged here.
/// </summary>
public static class ComponentOrdersTiming
{
    public static Activity StartOperation() => new Activity("ComponentOrders").Start();

    public static async Task<T> MeasureAsync<T>(ILogger logger, string phase, Func<Task<T>> action,
        int? inputCount = null, Func<T, int>? count = null, Func<T, string>? outcome = null, int? index = null)
    {
        using var activity = new Activity(phase).Start();
        var started = Stopwatch.GetTimestamp();
        var category = "Failed";
        int? outputCount = null;
        try
        {
            var result = await action();
            outputCount = count?.Invoke(result);
            category = outcome?.Invoke(result) ?? "Succeeded";
            return result;
        }
        catch (OperationCanceledException)
        {
            category = "Cancelled";
            throw;
        }
        finally
        {
            try
            {
                logger.LogInformation(
                    "Component Orders timing. OperationId={OperationId} SpanId={SpanId} ParentSpanId={ParentSpanId} Phase={Phase} Outcome={Outcome} InputCount={InputCount} OutputCount={OutputCount} Index={Index} PhaseElapsedMs={PhaseElapsedMs}",
                    activity.TraceId.ToString(), activity.SpanId.ToString(), activity.ParentSpanId.ToString(),
                    phase, category, inputCount, outputCount, index, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }
            catch
            {
                // Diagnostics must not replace source cancellation/failure or interrupt resource handoff.
            }
        }
    }

    public static T Measure<T>(ILogger logger, string phase, Func<T> action,
        int? inputCount = null, Func<T, int>? count = null, Func<T, string>? outcome = null) =>
        MeasureAsync(logger, phase, () => Task.FromResult(action()), inputCount, count, outcome).GetAwaiter().GetResult();
}
