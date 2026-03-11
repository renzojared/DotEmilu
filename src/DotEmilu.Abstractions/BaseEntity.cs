namespace DotEmilu.Abstractions;

/// <summary>
/// Represents an entity with soft delete support.
/// Indicates whether the entity is marked as deleted without being physically removed from the database.
/// </summary>
public interface IBaseEntity
{
    bool IsDeleted { get; set; }
}

/// <summary>
/// Represents the type of the primary key for the entity.
/// It is recommended to use numeric types (such as int, long) or Guid for efficient identifiers.
/// Although any struct type is allowed, using common types facilitates interoperability and performance.
/// </summary>
/// <typeparam name="TKey">Type of the primary key.</typeparam>
public interface IBaseEntity<TKey> : IBaseEntity
    where TKey : struct
{
    TKey Id { get; set; }
}

/// <summary>
/// Abstract base class that provides common properties for entities with a struct-based primary key.
/// Designed to be inherited by entities that require an identifier and the ability to mark records as deleted.
/// </summary>
/// <typeparam name="TKey">Type of the primary key for the entity.</typeparam>
public abstract class BaseEntity<TKey> : IBaseEntity<TKey>
    where TKey : struct
{
    public TKey Id { get; set; }
    public bool IsDeleted { get; set; }
}