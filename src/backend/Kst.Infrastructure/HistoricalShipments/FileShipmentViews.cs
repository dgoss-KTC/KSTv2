using Kst.Application.HistoricalShipments;
using Kst.Domain.HistoricalShipments;

namespace Kst.Infrastructure.HistoricalShipments;

public sealed partial class FileHistoricalShipmentsStore
{
    private static ShipmentView CurrentView(Entry e)
    {
        if (e.Operation.State != "ready") throw new ShipmentOperationException(409, "Complete acquisition is not ready.");
        return e.View ?? new(e.Id, 0, e.Operation.RowCount, e.Operation.OnTimeNumerator ?? 0, e.Operation.OnTimeDenominator ?? 0, []);
    }
    private void CheckView(Entry e, long version)
    {
        if (e.Released || e.Expires <= _clock.GetUtcNow())
            throw new ShipmentOperationException(410, "Operation expired or released.");
        if (version < 0 || CurrentView(e).Version != version)
            throw new ShipmentOperationException(409, "The view changed. Reload the current view before requesting rows or values.");
    }
    private static void Sequence(long sequence)
    {
        if (sequence < 1 || sequence > 9007199254740991L) throw new ArgumentException("Invalid view request sequence.");
    }
    public ShipmentView GetView(Guid id, string owner)
    {
        var e = Lease(id, owner);
        try { lock (_gate) return CurrentView(e); }
        finally { Unlease(e); }
    }
    public void CancelView(Guid id, string owner, long sequence)
    {
        Sequence(sequence);
        var e = Lease(id, owner);
        try
        {
            lock (_gate)
            {
                if (sequence < e.ViewSequence) return;
                e.ViewSequence = sequence;
                e.ViewCancel?.Cancel();
            }
        }
        finally { Unlease(e); }
    }
    public async Task<ShipmentView> SetViewAsync(Guid id, string owner, long expectedVersion, long sequence,
        IReadOnlyList<ShipmentViewFilter> filters, CancellationToken ct)
    {
        var e = Lease(id, owner); var entered = false; var worker = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, e.Cancel.Token);
        var pending = Path.Combine(e.Directory, "view.pending");
        try
        {
            Sequence(sequence); filters = ShipmentViewRules.Validate(filters);
            lock (_gate)
            {
                CheckView(e, expectedVersion);
                if (sequence <= e.ViewSequence) throw new ShipmentOperationException(409, "View request was superseded or cancelled.");
                // Reserve the sequence before work. A cancel arriving before this request prevents it
                // starting; an older delayed request can never replace a newer successful view.
                e.ViewSequence = sequence;
            }
            if (!await e.Gate.WaitAsync(0, linked.Token)) throw new ShipmentOperationException(409, "Operation is busy. Retry after the current work settles.");
            entered = true;
            lock (_gate)
            {
                CheckView(e, expectedVersion);
                if (e.ViewSequence != sequence) throw new OperationCanceledException(linked.Token);
                e.ViewCancel = linked;
            }
            worker = await _workers.WaitAsync(0, linked.Token);
            if (!worker) throw new ShipmentOperationException(503, "Shipment workers are busy. Retry.");
            var matches = ShipmentViewRules.Predicate(e.Operation.Columns, filters);
            var onTime = e.Operation.Columns.ToList().IndexOf("onTime");
            long count = 0, numerator = 0, denominator = 0;
            using (var reader = new BinaryReader(OpenRead(e.Rows)))
            using (var output = new BinaryWriter(new FileStream(pending, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                for (long i = 0; i < e.Operation.RowCount; i++)
                {
                    linked.Token.ThrowIfCancellationRequested();
                    var offset = reader.BaseStream.Position; var row = ReadRow(reader);
                    if (matches(row))
                    {
                        output.Write(offset); count++;
                        if (onTime >= 0 && row.Cells[onTime] is "1" or "0")
                        { denominator++; if (row.Cells[onTime] == "1") numerator++; }
                    }
                    if (i % 256 == 0) await Task.Yield();
                }
                if (reader.BaseStream.Position != reader.BaseStream.Length) throw new InvalidDataException("Frozen result is incomplete.");
                output.Flush();
            }
            lock (_gate)
            {
                linked.Token.ThrowIfCancellationRequested(); CheckView(e, expectedVersion);
                if (e.ViewSequence != sequence) throw new OperationCanceledException(linked.Token);
                var view = new ShipmentView(id, checked(expectedVersion + 1), count, numerator, denominator, filters);
                File.Move(pending, e.ViewIndex, true); // metadata and complete index publish under one lock/gate
                e.View = view;
                return view;
            }
        }
        finally
        {
            if (entered)
            {
                lock (_gate) { if (ReferenceEquals(e.ViewCancel, linked)) e.ViewCancel = null; }
                // Failure/cancellation never releases the acquisition or replaces its last good view.
                try { File.Delete(pending); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
            if (worker) _workers.Release();
            if (entered) e.Gate.Release();
            Unlease(e);
        }
    }
    public async Task<WorkspaceShipmentPage> WorkspacePageAsync(Guid id, string owner, long version, long offset, int limit, CancellationToken ct)
    {
        if (offset < 0 || offset > long.MaxValue / sizeof(long) || limit < 1 || limit > ShipmentViewRules.MaximumPageSize)
            throw new ArgumentException("Invalid page offset/limit.");
        var e = Lease(id, owner); var entered = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, e.Cancel.Token);
        try
        {
            if (!await e.Gate.WaitAsync(0, linked.Token)) throw new ShipmentOperationException(409, "Operation is busy. Retry.");
            entered = true;
            ShipmentView view; lock (_gate) { CheckView(e, version); view = CurrentView(e); }
            var rows = new List<ShipmentRow>(limit);
            if (offset < view.RowCount)
            {
                using var index = new BinaryReader(OpenRead(e.View is null ? e.Index : e.ViewIndex));
                using var reader = new BinaryReader(OpenRead(e.Rows));
                index.BaseStream.Position = offset * sizeof(long);
                for (var i = offset; i < view.RowCount && rows.Count < limit; i++)
                {
                    linked.Token.ThrowIfCancellationRequested(); reader.BaseStream.Position = index.ReadInt64(); rows.Add(ReadRow(reader));
                }
            }
            lock (_gate) { linked.Token.ThrowIfCancellationRequested(); CheckView(e, version); return new(e.Operation, view, offset, rows.AsReadOnly()); }
        }
        finally { if (entered) e.Gate.Release(); Unlease(e); }
    }
    public async Task<ShipmentFacets> FacetsAsync(Guid id, string owner, long version, string column, string? search,
        IReadOnlyList<string> retainedValues, CancellationToken ct)
    {
        var e = Lease(id, owner); var entered = false; var worker = false;
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, e.Cancel.Token);
        try
        {
            var metadata = ShipmentViewRules.Column(column); ShipmentViewRules.TextBound(search);
            if (column != "onTime" && metadata.Kind != "text") throw new ArgumentException("This column does not support value lists.");
            if (retainedValues is null || retainedValues.Count > ShipmentViewRules.MaximumValues || retainedValues.Distinct(StringComparer.Ordinal).Count() != retainedValues.Count)
                throw new ArgumentException("Invalid retained values list.");
            foreach (var value in retainedValues)
            { if (string.IsNullOrEmpty(value)) throw new ArgumentException("Blank values are separate."); ShipmentViewRules.TextBound(value); }
            if (!await e.Gate.WaitAsync(0, linked.Token)) throw new ShipmentOperationException(409, "Operation is busy. Retry.");
            entered = true;
            ShipmentView view; lock (_gate) { CheckView(e, version); view = CurrentView(e); }
            worker = await _workers.WaitAsync(0, linked.Token);
            if (!worker) throw new ShipmentOperationException(503, "Shipment workers are busy. Retry.");
            var predicate = ShipmentViewRules.Predicate(e.Operation.Columns, view.Filters, column);
            var index = e.Operation.Columns.ToList().IndexOf(column);
            var retained = new HashSet<string>(retainedValues, StringComparer.Ordinal);
            foreach (var value in view.Filters.SingleOrDefault(f => f.Column == column)?.Values ?? []) retained.Add(value);
            using var sort = new ShipmentFacetSort(Path.Combine(e.Directory, "facet-scratch"));
            long blanks = 0;
            await foreach (var row in ReadRows(e, linked.Token))
            {
                if (!predicate(row)) continue;
                var value = row.Cells[index];
                if (string.IsNullOrEmpty(value)) blanks++;
                else await sort.AddAsync(value, linked.Token);
            }
            var result = await sort.CompleteAsync(id, version, column, search, retained, blanks, linked.Token);
            lock (_gate) { linked.Token.ThrowIfCancellationRequested(); CheckView(e, version); return result; }
        }
        finally { if (worker) _workers.Release(); if (entered) e.Gate.Release(); Unlease(e); }
    }
}
