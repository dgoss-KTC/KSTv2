namespace Kst.Domain.Shortages;

/// <summary>Stage 9 material result state. Manufactured components remain visible but do not participate in the purchased-material shortage calculation.</summary>
public enum MaterialStatus
{
    Unknown,
    Short,
    OnHand,
    NotApplicable
}
