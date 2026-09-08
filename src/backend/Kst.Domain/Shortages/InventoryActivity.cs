namespace Kst.Domain.Shortages;

/// <summary>Non-usable inventory context classifications retained for explanation, never as Stage 9 coverage.</summary>
public enum InventoryActivity
{
    Transit,
    Inspection,
    NonNet,
    Mrb,
    NcmInspection,
    ExpiredExpiring
}
