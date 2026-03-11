namespace DotEmilu.Abstractions;

/// <summary>
/// Represents the type of the user identifier performing the audit action.
/// It is recommended to use numeric types (such as int, long) or Guid for efficient user identification.
/// Although any struct type is allowed, using common types facilitates interoperability and performance.
/// </summary>
/// <typeparam name="TUserKey">Type of the audit user identifier.</typeparam>
public interface IBaseAuditableEntity<TUserKey> : IBaseEntity
    where TUserKey : struct
{
    public DateTimeOffset Created { get; set; }
    public TUserKey CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public TUserKey LastModifiedBy { get; set; }
    public DateTimeOffset? Deleted { get; set; }
    public TUserKey? DeletedBy { get; set; }
}

/// <summary>
/// Abstract base class that provides common properties for entities requiring auditing.
/// Designed to be inherited by entities that need to record information about record creation, modification, and deletion.
/// </summary>
/// <typeparam name="TUserKey">Type of the audit user identifier.</typeparam>
public abstract class BaseAuditableEntity<TUserKey> : IBaseAuditableEntity<TUserKey>
    where TUserKey : struct
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset Created { get; set; }
    public TUserKey CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public TUserKey LastModifiedBy { get; set; }
    public DateTimeOffset? Deleted { get; set; }
    public TUserKey? DeletedBy { get; set; }
}

/// <summary>
/// Represents an auditable entity that combines base entity properties with audit tracking information.
/// This interface extends both IBaseEntity&lt;TKey&gt; and IBaseAuditableEntity&lt;TUserKey&gt;, providing
/// a unified contract for entities that require both a primary key and audit details.
/// </summary>
/// <typeparam name="TKey">The type of the primary key for the entity.</typeparam>
/// <typeparam name="TUserKey">The type of the user identifier for audit tracking.</typeparam>
public interface IBaseAuditableEntity<TKey, TUserKey> : IBaseEntity<TKey>, IBaseAuditableEntity<TUserKey>
    where TKey : struct
    where TUserKey : struct;

/// <summary>
/// Abstract base class that combines the properties of a base entity and an auditable entity.
/// Designed to be inherited by entities that require both an identifier and audit information.
/// </summary>
/// <typeparam name="TKey">Type of the primary key for the entity.</typeparam>
/// <typeparam name="TUserKey">Type of the audit user identifier.</typeparam>
public abstract class BaseAuditableEntity<TKey, TUserKey> : IBaseAuditableEntity<TKey, TUserKey>
    where TKey : struct
    where TUserKey : struct
{
    public TKey Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset Created { get; set; }
    public TUserKey CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public TUserKey LastModifiedBy { get; set; }
    public DateTimeOffset? Deleted { get; set; }
    public TUserKey? DeletedBy { get; set; }
}