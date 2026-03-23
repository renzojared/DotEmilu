using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.EntityFrameworkCore.Mocks;

/// <summary>
/// Mock implementation of <see cref="IAppUserContext"/> for standalone EF Core demonstrations.
/// </summary>
public class MockContextUser : IAppUserContext
{
    /// <inheritdoc />
    public Guid Id { get; } = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
}
