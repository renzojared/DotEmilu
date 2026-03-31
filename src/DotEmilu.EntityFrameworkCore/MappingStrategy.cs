namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// Specifies the inheritance mapping strategy used by Entity Framework Core.
/// </summary>
public enum MappingStrategy
{
    /// <summary>Table-per-concrete-type (TPC) mapping strategy.</summary>
    Tpc = 1,

    /// <summary>Table-per-hierarchy (TPH) mapping strategy.</summary>
    Tph = 2,

    /// <summary>Table-per-type (TPT) mapping strategy.</summary>
    Tpt = 3
}