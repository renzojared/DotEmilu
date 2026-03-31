using DotEmilu.Abstractions;

namespace DotEmilu.Samples.EntityFrameworkCore.Scenarios.S02AuditableReflection;

/// <summary>
/// Represents a system user with an integer key.
/// Discovered automatically by <c>AddAuditableEntityInterceptors</c> via assembly scanning,
/// alongside <see cref="Mocks.MockContextUser"/> which implements <see cref="IContextUser{TUserKey}"/> for <see cref="Guid"/>.
/// </summary>
internal sealed class SystemUser : IContextUser<int>
{
    /// <inheritdoc />
    public int Id => 42;
}
