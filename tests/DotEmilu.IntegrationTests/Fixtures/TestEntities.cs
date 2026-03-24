namespace DotEmilu.IntegrationTests.Fixtures;

/// <summary>
/// Simple entity for testing <see cref="DotEmilu.EntityFrameworkCore.SoftDeleteInterceptor"/>.
/// </summary>
public class TestEntity : BaseEntity<Guid>
{
    public required string Name { get; set; }
}

/// <summary>
/// Auditable entity for testing <see cref="DotEmilu.EntityFrameworkCore.AuditableEntityInterceptor{TUserKey}"/>.
/// </summary>
public class TestAuditableEntity : BaseAuditableEntity<Guid, Guid>
{
    public required string Title { get; set; }
}

/// <summary>
/// Fake context user that returns a fixed Guid for test assertions.
/// </summary>
public class TestContextUser : IContextUser<Guid>
{
    public static readonly Guid FixedUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    public Guid Id => FixedUserId;
}
