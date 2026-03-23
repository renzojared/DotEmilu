using System.Reflection;

namespace DotEmilu.Samples.Domain;

/// <summary>
/// Provides a stable reference to the Domain assembly.
/// Useful for dependency injection or scanning (e.g., FluentValidation, MediatR, or EF Core configurations)
/// without coupling to a specific entity.
/// </summary>
public static class DomainAssembly
{
    /// <summary>
    /// Gets the current assembly where the domain models and contracts reside.
    /// </summary>
    public static readonly Assembly Instance = typeof(DomainAssembly).Assembly;
}
