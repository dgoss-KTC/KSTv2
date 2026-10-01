namespace Kst.Domain.OpenOrders;

/// <summary>A proposal retains the original facts even when the source line disappears.</summary>
public sealed record OpenOrderProposal(OpenOrderLineKey Key, string Site, string ItemNumber,
    OpenOrderEditableValues Original, OpenOrderEditableValues Proposed, string? ReasonCode);

public static class OpenOrderPlanning
{
    public static readonly IReadOnlySet<string> ReasonCodes = new HashSet<string>(StringComparer.Ordinal)
        { "Cust/PM", "Buyers", "Planning", "Factory", "C&R", "Quality", "Engineer" };

    public static bool Changed(OpenOrderProposal proposal) => proposal.Original != proposal.Proposed;

    public static decimal ProposedOpen(OpenOrderProposal proposal, decimal shippedQty) => proposal.Proposed.OrderQty - shippedQty;
    public static decimal ProposedExtPrice(OpenOrderProposal proposal, decimal shippedQty) =>
        proposal.Proposed.Price * ProposedOpen(proposal, shippedQty);

    public static IReadOnlyList<string> Issues(OpenOrderProposal proposal, OpenOrderLine? current)
    {
        var issues = new List<string>();
        if (!Changed(proposal)) return issues;
        if (current is null) issues.Add("Line missing or no longer open in the current workspace report.");
        else
        {
            if (current.Site != proposal.Site || current.ItemNumber != proposal.ItemNumber || current.SourceValues != proposal.Original)
                issues.Add("Source identity or original editable values changed.");
            if (proposal.Proposed.OrderQty < current.ShippedQty)
                issues.Add("Order Qty cannot be below current Shipped Qty.");
        }
        if (proposal.ReasonCode is null) issues.Add("Reason Code is required.");
        else if (!ReasonCodes.Contains(proposal.ReasonCode)) issues.Add("Reason Code is invalid.");
        return issues;
    }
}
