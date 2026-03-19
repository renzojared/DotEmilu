namespace DotEmilu.Samples.Domain.Contracts;

/// <summary>
/// Domain contract representing the current authenticated user.
/// </summary>
public interface IAppUserContext : IContextUser<Guid>;
