---
title: "Entity Framework Core Patterns"
description: "Complete guide to DotEmilu.EntityFrameworkCore utilities: auditable entity interceptors, soft delete, base configurations, mapping strategies, CQRS patterns, and pagination."
---

# Entity Framework Core patterns

This guide covers all EF Core utilities provided by `DotEmilu.EntityFrameworkCore`: auditable entities, soft delete, base configurations, mapping strategies, CQRS patterns, and pagination.

## Auditable entities

### Defining auditable entities

Inherit from `BaseAuditableEntity<TKey, TUserKey>` to get automatic audit tracking:

```csharp
// FILE: samples/DotEmilu.Samples.Domain/Entities/Invoice.cs
// TKey = primary key type (int), TUserKey = user identifier type (Guid)
public class Invoice : BaseAuditableEntity<int, Guid>
{
    public string Number { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public bool IsPaid { get; set; }
}
```

This gives your entity these properties automatically:
- `Id` (int) — primary key
- `IsDeleted` (bool) — soft-delete flag
- `Created` (DateTimeOffset) — creation timestamp
- `CreatedBy` (Guid) — who created it
- `LastModified` (DateTimeOffset) — last modification timestamp
- `LastModifiedBy` (Guid) — who last modified it
- `Deleted` (DateTimeOffset?) — deletion timestamp
- `DeletedBy` (Guid?) — who deleted it

### Non-auditable entities with soft delete

For entities that need soft delete but not full audit tracking:

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/Entities/Song.cs
public class Song : BaseEntity<Guid>
{
    public string Name { get; set; } = default!;
    public SongType SongType { get; set; }
}
```

This gives: `Id` (Guid), `IsDeleted` (bool).

### Auditable entities without a primary key

For owned types or join entities that need audit fields but no `Id`:

```csharp
// FILE: src/DotEmilu.Abstractions/BaseAuditableEntity.cs
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
```

This gives all audit fields (`Created`, `CreatedBy`, etc.) but no `Id` property.

## AuditableEntityInterceptor

Automatically populates audit fields on `SaveChanges`/`SaveChangesAsync`:

| Entity state | Fields set |
|---|---|
| `Added` | `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy` |
| `Modified` | `LastModified`, `LastModifiedBy` |
| `Deleted` (or `Modified` with `IsDeleted = true`) | `IsDeleted = true`, `Deleted`, `DeletedBy`, `LastModified`, `LastModifiedBy` (state changed to `Unchanged`) |

### Implementing IContextUser

The interceptor needs `IContextUser<TUserKey>` to know who the current user is:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Infrastructure/Auth/CurrentUser.cs
// For ASP.NET Core — resolve from HttpContext claims
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : IAppUserContext
{
    private static readonly Guid DemoUserId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    public Guid Id
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out var userId)
                ? userId
                : DemoUserId;
        }
    }
}
```

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/Mocks/MockContextUser.cs
// For background services or tests — use a static/mock user
public class MockContextUser : IAppUserContext
{
    public Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
}
```

### Registration

**Option A: Explicit registration** (specify exactly which implementation to use)

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/Scenarios/S01AuditableExplicit/Runner.cs
services.AddAuditableEntityInterceptor<MockContextUser, Guid>();
```

**Option B: Assembly-scanning** (discovers all `IContextUser<>` implementations)

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/DataAccessContainer.cs
services.AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly());
```

> Assembly scanning throws `ArgumentException` if multiple `IContextUser<T>` implementations exist for the same `TUserKey` type.

## SoftDeleteInterceptor

Converts `EntityState.Deleted` → `EntityState.Unchanged` and sets `IsDeleted = true` for all `IBaseEntity` implementations:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/DataAccessContainer.cs
services.AddSoftDeleteInterceptor();
```

This works independently from `AuditableEntityInterceptor`. Use both together for full audit + soft delete:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/DataAccessContainer.cs
services
    .AddSoftDeleteInterceptor()
    .AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly());
```

### How they interact

When both interceptors are registered and you call `db.Remove(entity)`:

1. `SoftDeleteInterceptor` runs first: detects `EntityState.Deleted`, changes state to `Unchanged`, sets `IsDeleted = true`
2. EF Core's `AutoDetectChanges` sees that `IsDeleted` changed — marks the entity as `Modified`
3. `AuditableEntityInterceptor` runs: matches `case EntityState.Modified when entity.IsDeleted`, fills `Deleted`, `DeletedBy`, `LastModified`, `LastModifiedBy`

If only `AuditableEntityInterceptor` is registered (without `SoftDeleteInterceptor`), it handles the `Deleted` state directly via `case EntityState.Deleted:` and produces the same result — no `SoftDeleteInterceptor` needed for auditable entities.

If only `SoftDeleteInterceptor` is registered, it handles the state change but doesn't set audit fields.

## Base entity configurations

### BaseEntityConfiguration

Configures entities implementing `IBaseEntity<TKey>`:

- Sets `Id` as primary key with `ValueGeneratedOnAdd`
- Configures `IsDeleted` column (with default value, query filter, and index)
- Optionally adds a `RowVersion` column
- Applies the specified mapping strategy (TPC/TPH/TPT)

### BaseAuditableEntityConfiguration

Configures entities implementing `IBaseAuditableEntity<TUserKey>`:

- Configures all audit columns (`Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`, `Deleted`, `DeletedBy`)
- Sets `HasNoKey()` (designed for use with the generic overload `BaseAuditableEntity<TKey, TUserKey>` where the key comes from `BaseEntityConfiguration`)
- Configures `IsDeleted` column
- Applies the specified mapping strategy

### Applying configurations

> **Order in `OnModelCreating` is critical.** `BaseAuditableEntityConfiguration` calls `HasNoKey()` to configure audit columns. `BaseEntityConfiguration` then restores `HasKey()` and defines the primary key. Reversing these two steps causes auditable entities to lose their primary key. Always follow the order: `ApplyBaseAuditableEntityConfiguration` → `ApplyBaseEntityConfiguration` → `ApplyConfigurationsFromAssembly`.

**Option A: Generic type parameter** (apply to all entities with a specific key type)

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/Scenarios/S07ModelBuilderOverloads/Runner.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure all IBaseAuditableEntity<Guid> entities
    modelBuilder.ApplyBaseAuditableEntityConfiguration<Guid>();

    // Configure all IBaseEntity<int> and IBaseEntity<Guid> entities
    modelBuilder.ApplyBaseEntityConfiguration<int>();
    modelBuilder.ApplyBaseEntityConfiguration<Guid>();

    modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);
}
```

**Option B: Assembly-scanning** (discovers key types automatically)

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Infrastructure/Persistence/InvoiceDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    var domainAssembly = DomainAssembly.Instance;
    var currentAssembly = Assembly.GetExecutingAssembly();

    modelBuilder
        // 1) Auditable first: applies audit fields and temporary keyless behavior.
        .ApplyBaseAuditableEntityConfiguration(domainAssembly,
            new Dictionary<Type, MappingStrategy> { { typeof(Guid), MappingStrategy.Tph } })
        // 2) Base entity second: restores key + applies final mapping strategy and soft-delete base config.
        .ApplyBaseEntityConfiguration(domainAssembly,
            new Dictionary<Type, (MappingStrategy, bool)> { { typeof(int), (MappingStrategy.Tph, false) } })
        // 3) Business config last so custom rules can override defaults.
        .ApplyConfigurationsFromAssembly(currentAssembly);
}
```

**Option C: Assembly-scanning with per-type customization**

```csharp
modelBuilder.ApplyBaseEntityConfiguration(
    typeof(Invoice).Assembly,
    keyTypeConfigurations: new Dictionary<Type, (MappingStrategy, bool)>
    {
        [typeof(int)] = (MappingStrategy.Tpc, false),        // int keys: TPC, no row version
        [typeof(Guid)] = (MappingStrategy.Tpt, true)          // Guid keys: TPT, with row version
    });

modelBuilder.ApplyBaseAuditableEntityConfiguration(
    typeof(Invoice).Assembly,
    userKeyConfigurations: new Dictionary<Type, MappingStrategy>
    {
        [typeof(Guid)] = MappingStrategy.Tpc
    });
```

## Mapping strategies

```csharp
// FILE: src/DotEmilu.EntityFrameworkCore/MappingStrategy.cs
public enum MappingStrategy
{
    Tpc = 1,  // Table-per-concrete-type (default)
    Tph = 2,  // Table-per-hierarchy
    Tpt = 3   // Table-per-type
}
```

Apply in custom configurations:

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/DataAccess/Configurations/SongConfiguration.cs
public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        builder.ApplyMappingStrategy(MappingStrategy.Tpt);
    }
}
```

### Strategy precedence for auditable entities

For an entity implementing `BaseAuditableEntity<TKey, TUserKey>`, `OnModelCreating` calls both configuration steps:

1. `ApplyBaseAuditableEntityConfiguration` applies a strategy keyed by `TUserKey`
2. `ApplyBaseEntityConfiguration` applies a strategy keyed by `TKey` — **this one wins**

The final strategy is always from step 2. The value from step 1 only takes effect if `ApplyBaseEntityConfiguration` is never called for that entity.

### When does the strategy actually matter?

`MappingStrategy` only has an observable effect on the database **when a real inheritance hierarchy exists** — an abstract base class with concrete subclasses registered in EF Core. For a flat entity with no subclasses, all three strategies produce the same table; EF Core ignores the hint. Declaring a strategy is still useful because it expresses intent for when the hierarchy grows and guarantees predictable behavior across EF Core version upgrades.

### Three levels of granularity

| Level | Mechanism | Scope |
|---|---|---|
| Global by key type | `{ typeof(int): (Tph, false) }` in the dictionary | All entities with that key type |
| Per individual entity | `builder.ApplyMappingStrategy(...)` in `IEntityTypeConfiguration<T>` | That entity only |
| Unspecified | Automatic TPC fallback | Everything else |

### Default: TPC, not TPH

This library defaults to **TPC** (Table-per-concrete-type). EF Core's own default is **TPH**. This is an intentional trade-off:

| Strategy | Behavior |
|---|---|
| **TPC** (library default) | One table per concrete type — no null columns, better for large tables with deep hierarchies |
| **TPH** (EF Core default) | Single table for the entire hierarchy — simpler, supports direct joins without UNION |

If you're adopting this library in an existing project, override the default explicitly via the `keyTypeConfigurations` dictionary to avoid unexpected table layout changes.

## IsDeleted column options

The `UseIsDeleted` extension provides fine-grained control:

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/DataAccess/Configurations/SongConfiguration.cs
builder.UseIsDeleted(
    useShort: true,         // store as short (0/1) instead of bool
    useIndex: true,         // create index on IsDeleted column
    useQueryFilter: true,   // add global query filter: WHERE IsDeleted = false
    order: 1                // column order (0 = no specific order)
);
```

**Short conversion** is useful for databases that don't natively support boolean columns:

```csharp
builder.UseIsDeleted(useShort: true);
// Column type: short, default value: 0
// Comment: "Soft delete: 1 is deleted"
```

**Bool mode** (default):

```csharp
builder.UseIsDeleted();
// Column type: bool, default value: false
// Comment: "Soft delete: true is deleted"
```

## Enum column comments

Auto-generate SQL comments from enum values:

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/DataAccess/Configurations/SongConfiguration.cs
builder.Property(s => s.SongType)
    .HasFormattedComment()                          // "0 = Rock, 1 = Pop, 2 = Jazz"
    .IsRequired();

builder.Property(s => s.SongType)
    .HasFormattedComment("{0} = {2}")               // "0 = Rock music, 1 = Pop music, 2 = Jazz"
    .IsRequired();

builder.Property(s => s.SongType)
    .HasFormattedComment("{0} = {2}", includeTitle: true);  // Uses [Description] on the enum type
```

**Format placeholders:**
- `{0}` — Numeric value
- `{1}` — Enum name
- `{2}` — `[Description]` attribute value (falls back to enum name)

Works with both `PropertyBuilder<TEnum>` and `PropertyBuilder<TEnum?>`.

## Bool-to-short conversion

```csharp
// FILE: src/DotEmilu.EntityFrameworkCore/Extensions/PropertyBuilderExtensions.cs
builder.Property(s => s.IsDeleted)
    .HasShortConversion();
// Converts: true ↔ (short)1, false ↔ (short)0
```

## Pagination

Convert any `IQueryable<T>` to a paginated result:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/GetInvoices/GetInvoices.cs
await db.Invoices
    .OrderByDescending(i => i.Created)
    .AsPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
```

**`PaginatedList<T>` properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyCollection<T>` | Items on the current page |
| `PageNumber` | `int` | Current page (1-based) |
| `TotalPages` | `int` | Total pages |
| `TotalCount` | `int` | Total items across all pages |
| `HasPreviousPage` | `bool` | `PageNumber > 1` |
| `HasNextPage` | `bool` | `PageNumber < TotalPages` |

### Querying soft-deleted records

By default, the global query filter excludes soft-deleted entities. To include them:

```csharp
// FILE: samples/DotEmilu.Samples.EntityFrameworkCore/Scenarios/S04PaginatedList/Runner.cs
var allInvoices = await db.Invoices
    .IgnoreQueryFilters()
    .AsPaginatedListAsync(pageNumber: 1, pageSize: 10, cancellationToken);
```

## IUnitOfWork

Abstraction for `SaveChangesAsync` — implement it on your DbContext or CQRS command interface:

```csharp
// FILE: src/DotEmilu.EntityFrameworkCore/IUnitOfWork.cs
public interface IUnitOfWork
{
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## CQRS pattern

Separate read and write concerns using interface segregation:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Infrastructure/Persistence/CqrsInterfaces.cs
// Shared entity definitions
public interface IInvoiceEntities
{
    DbSet<Invoice> Invoices { get; }
}

// Commands: tracked, read-write, includes SaveChangesAsync
public interface IInvoiceCommands : IInvoiceEntities, IUnitOfWork;

// Queries: no-tracking, read-only
public interface IInvoiceQueries : IInvoiceEntities;
```

Implement with one DbContext, two registrations:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Infrastructure/Persistence/CqrsContexts.cs
// Command context — default tracking behavior
internal sealed class InvoiceCommands(DbContextOptions<InvoiceDbContext> options)
    : InvoiceDbContext(options), IInvoiceCommands;

// Query context — no-tracking by default
internal sealed class InvoiceQueries : InvoiceDbContext, IInvoiceQueries
{
    public InvoiceQueries(DbContextOptions<InvoiceDbContext> options) : base(options)
        => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
}
```

Register:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/DataAccessContainer.cs
services.AddScoped<IInvoiceCommands, InvoiceCommands>();
services.AddScoped<IInvoiceQueries, InvoiceQueries>();
```

Use in handlers:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/CreateInvoice.cs
// Write handler — uses IInvoiceCommands (tracked + SaveChangesAsync)
internal sealed class Handler(IVerifier<CreateInvoiceRequest> verifier, IInvoiceCommands db)
    : Handler<CreateInvoiceRequest>(verifier) { ... }
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/GetInvoices/GetInvoices.cs
// Read handler — uses IInvoiceQueries (no tracking)
internal sealed class Handler(IVerifier<GetInvoicesRequest> verifier, IInvoiceQueries db)
    : Handler<GetInvoicesRequest, PaginatedList<Invoice>>(verifier) { ... }
```

## DI registration summary

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/DataAccessContainer.cs
services
    // Soft-delete interceptor
    .AddSoftDeleteInterceptor()

    // Auditable interceptors (scan assembly for IContextUser<> implementations)
    .AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly())

    // Or register explicitly:
    // .AddAuditableEntityInterceptor<CurrentUser, Guid>()

    // DbContext with interceptors
    .AddDbContext<InvoiceDbContext>((sp, options) =>
        options
            .UseInMemoryDatabase("FullAppInvoicesDb")
            .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));
```
