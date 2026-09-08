namespace Kst.Domain.Shortages;

/// <summary>Identifies whether a component requirement came from actual WO detail or an effective BOM projection.</summary>
public enum RequirementSource
{
    ActualWo,
    ProjectedBom
}
