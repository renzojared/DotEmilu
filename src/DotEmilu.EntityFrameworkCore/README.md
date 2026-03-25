# DotEmilu.EntityFrameworkCore

[![NuGet](https://img.shields.io/nuget/v/DotEmilu.EntityFrameworkCore.svg)](https://www.nuget.org/packages/DotEmilu.EntityFrameworkCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Entity Framework Core utilities for **DotEmilu** — auditable entity interceptors, soft-delete interceptor, base entity configurations with TPC/TPH/TPT mapping strategies, pagination, and CQRS-ready patterns.

## Install

```bash
dotnet add package DotEmilu.EntityFrameworkCore
```

This transitively installs `DotEmilu.Abstractions`.

## What's included

### Interceptors

| Type | Purpose |
|------|---------|
| `AuditableEntityInterceptor<TUserKey>` | Automatically sets `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`, `Deleted`, `DeletedBy` on `SaveChanges` using `IContextUser<TUserKey>` and `TimeProvider` |
| `SoftDeleteInterceptor` | Converts `EntityState.Deleted` to `EntityState.Unchanged` + `IsDeleted = true` for all `IBaseEntity` implementations |

### Entity configurations

| Type | Purpose |
|------|---------|
| `BaseEntityConfiguration<TEntity, TKey>` | Configures primary key, `ValueGeneratedOnAdd`, `IsDeleted` column, optional `RowVersion`, and mapping strategy |
| `BaseAuditableEntityConfiguration<TEntity, TUserKey>` | Configures all audit columns (`Created`, `CreatedBy`, etc.), `IsDeleted`, and mapping strategy |

### Mapping strategies

| Enum value | Strategy |
|------------|----------|
| `MappingStrategy.Tpc` | Table-per-concrete-type (default) |
| `MappingStrategy.Tph` | Table-per-hierarchy |
| `MappingStrategy.Tpt` | Table-per-type |

### Extension methods

**EntityTypeBuilder extensions:**

| Method | Purpose |
|--------|---------|
| `builder.UseIsDeleted(useShort, useIndex, useQueryFilter, order)` | Configures `IsDeleted` column — optional short (0/1) conversion, global query filter, index |
| `builder.ApplyMappingStrategy(strategy)` | Applies TPC/TPH/TPT to the entity |

**PropertyBuilder extensions:**

| Method | Purpose |
|--------|---------|
| `property.HasShortConversion()` | Converts `bool` to `short` (0/1) in the database |
| `property.HasFormattedComment(format, includeTitle)` | Auto-generates SQL column comments from enum values and `[Description]` attributes |

**ModelBuilder extensions:**

| Method | Purpose |
|--------|---------|
| `modelBuilder.ApplyBaseEntityConfiguration<TKey>(strategy, enableRowVersion)` | Applies `BaseEntityConfiguration` to all `IBaseEntity<TKey>` types in the model |
| `modelBuilder.ApplyBaseEntityConfiguration(assembly, keyTypeConfigurations?)` | Assembly-scanning overload with per-key-type customization |
| `modelBuilder.ApplyBaseAuditableEntityConfiguration<TUserKey>(strategy)` | Applies `BaseAuditableEntityConfiguration` to all `IBaseAuditableEntity<TUserKey>` types |
| `modelBuilder.ApplyBaseAuditableEntityConfiguration(assembly, userKeyConfigurations?)` | Assembly-scanning overload |

**IQueryable extensions:**

| Method | Purpose |
|--------|---------|
| `source.AsPaginatedListAsync(pageNumber, pageSize, ct)` | Converts an `IQueryable<T>` to `PaginatedList<T>` using EF Core's `CountAsync`/`ToListAsync` |

### Contracts

| Type | Purpose |
|------|---------|
| `IUnitOfWork` | Exposes `Database` and `SaveChangesAsync()` for unit-of-work pattern |

### DI registration

| Method | Purpose |
|--------|---------|
| `services.AddSoftDeleteInterceptor()` | Registers `SoftDeleteInterceptor` as scoped `ISaveChangesInterceptor` |
| `services.AddAuditableEntityInterceptor<TContextUser, TUserKey>()` | Registers a specific `AuditableEntityInterceptor<TUserKey>` + `IContextUser` + `TimeProvider` |
| `services.AddAuditableEntityInterceptors(assembly)` | Scans assembly for `IContextUser<>` implementations and registers interceptors for each user key type |

## Auditable entities

```csharp
// Define an auditable entity
public class Invoice : BaseAuditableEntity<int, Guid>
{
    public string Number { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
}

// Implement IContextUser<Guid> for audit tracking
public class CurrentUser(IHttpContextAccessor accessor) : IContextUser<Guid>
{
    public Guid Id => Guid.Parse(accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// Register interceptors
services
    .AddSoftDeleteInterceptor()
    .AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly());
```

## DbContext configuration

```csharp
public class InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : DbContext(options)
{
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-configure audit columns for all IBaseAuditableEntity<Guid> entities
        modelBuilder.ApplyBaseAuditableEntityConfiguration<Guid>();

        // Auto-configure base entity columns from assembly
        modelBuilder.ApplyBaseEntityConfiguration(typeof(Invoice).Assembly);

        // Apply custom IEntityTypeConfiguration<T> from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InvoiceDbContext).Assembly);
    }
}
```

## Soft delete with IsDeleted column options

```csharp
public class SongConfiguration : IEntityTypeConfiguration<Song>
{
    public void Configure(EntityTypeBuilder<Song> builder)
    {
        // Store IsDeleted as short (0/1) instead of bool, with index and query filter
        builder.UseIsDeleted(useShort: true, useIndex: true, useQueryFilter: true, order: 1);

        // Apply TPT mapping strategy
        builder.ApplyMappingStrategy(MappingStrategy.Tpt);
    }
}
```

## Enum column comments

```csharp
// Automatically generate SQL comments from enum values
builder.Property(s => s.SongType)
    .HasFormattedComment("{0} = {1}")     // "0 = Rock, 1 = Pop, 2 = Jazz"
    .IsRequired();

// Include [Description] attributes
builder.Property(s => s.Status)
    .HasFormattedComment("{0} = {2}", includeTitle: true);  // "Order Status: 0 = Pending Order, 1 = Completed"
```

## Pagination

```csharp
var result = await db.Invoices
    .OrderBy(i => i.Id)
    .AsPaginatedListAsync(pageNumber: 1, pageSize: 10, cancellationToken);

// result.Items           → IReadOnlyCollection<Invoice>
// result.TotalCount      → total records
// result.TotalPages      → computed from count / pageSize
// result.PageNumber       → current page
// result.HasPreviousPage → bool
// result.HasNextPage     → bool
```

## CQRS pattern

Separate read and write concerns using interface segregation:

```csharp
// Commands interface (tracked, read-write)
public interface IInvoiceCommands : IInvoiceEntities, IUnitOfWork;

// Queries interface (no-tracking, read-only)
public interface IInvoiceQueries : IInvoiceEntities;

// Shared entity sets
public interface IInvoiceEntities
{
    DbSet<Invoice> Invoices { get; }
}
```

## Additional documentation

- [Getting started](https://github.com/renzojared/DotEmilu/blob/main/docs/getting-started.md)
- [Handler patterns](https://github.com/renzojared/DotEmilu/blob/main/docs/guides/handler-patterns.md)
- [Entity Framework Core patterns](https://github.com/renzojared/DotEmilu/blob/main/docs/guides/entity-framework-core-patterns.md)

## Part of the DotEmilu ecosystem

| Package | Description |
|---------|-------------|
| [DotEmilu.Abstractions](https://www.nuget.org/packages/DotEmilu.Abstractions) | Core interfaces and base classes |
| [DotEmilu](https://www.nuget.org/packages/DotEmilu) | Handler pipeline with FluentValidation |
| [DotEmilu.AspNetCore](https://www.nuget.org/packages/DotEmilu.AspNetCore) | ASP.NET Core Minimal API integration |
| **DotEmilu.EntityFrameworkCore** *(this)* | EF Core interceptors and configurations |

## Feedback

File bugs, feature requests, or questions on [GitHub Issues](https://github.com/renzojared/DotEmilu/issues).

[Full documentation & source](https://github.com/renzojared/DotEmilu)
