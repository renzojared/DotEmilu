# DotEmilu.Samples.EntityFrameworkCore

Console application demonstrating all `DotEmilu.EntityFrameworkCore` patterns with an in-memory SQLite database. Each scenario is self-contained.

## Run

```bash
cd samples/DotEmilu.Samples.EntityFrameworkCore
dotnet run
```

All 7 scenarios execute sequentially.

## Scenarios

### S01 — Auditable (Explicit Registration)

**Pattern:** `AddAuditableEntityInterceptor<TContextUser, TUserKey>()`

Registers `AuditableEntityInterceptor<Guid>` explicitly with a `MockContextUser`. Demonstrates:

- Insert → `Created` and `CreatedBy` populated automatically
- Update → `LastModified` and `LastModifiedBy` updated
- Soft-delete → `IsDeleted = true`, `Deleted` and `DeletedBy` set, entity state changed to `Unchanged`

### S02 — Auditable (Reflection/Assembly Scanning)

**Pattern:** `AddAuditableEntityInterceptors(assembly)`

Scans the assembly for `IContextUser<>` implementations and registers interceptors automatically. Demonstrates:

- Multiple `IContextUser<>` implementations (e.g., `MockContextUser : IContextUser<Guid>`, `SystemUser : IContextUser<int>`)
- Auto-discovery and registration of interceptors for each user key type

### S03 — Soft Delete

**Pattern:** `SoftDeleteInterceptor` standalone

Uses only `SoftDeleteInterceptor` (without `AuditableEntityInterceptor`). Demonstrates:

- `db.Remove(entity)` → entity state changes from `Deleted` to `Unchanged`, `IsDeleted = true`
- Entity no longer appears in queries (filtered by global query filter)
- Entity still exists in database (visible with `IgnoreQueryFilters()`)

### S04 — Paginated List

**Pattern:** `AsPaginatedListAsync()`

Demonstrates the `IQueryable<T>.AsPaginatedListAsync()` extension method:

- Seeds multiple invoices
- Queries with pagination: `pageNumber`, `pageSize`
- Shows `Items`, `TotalCount`, `TotalPages`, `HasPreviousPage`, `HasNextPage`
- Uses `IgnoreQueryFilters()` to include soft-deleted records

### S05 — CQRS Execution

**Pattern:** `ICommands` (tracked + `IUnitOfWork`) vs `IQueries` (no-tracking)

Demonstrates interface segregation for read/write:

- `ICommands` → tracked DbContext, includes `SaveChangesAsync`
- `IQueries` → no-tracking DbContext, read-only
- Shows that entities from `IQueries` are not tracked by the change tracker

### S06 — Model Configuration

**Pattern:** `UseIsDeleted()`, `HasFormattedComment()`, `HasShortConversion()`

Demonstrates custom entity configuration with:

- `UseIsDeleted(useShort: true)` — stores `IsDeleted` as `short` (0/1)
- `HasFormattedComment("{0} = {1}")` — auto-generates SQL column comments from enum values
- `ApplyMappingStrategy(MappingStrategy.Tpt)` — applies Table-per-Type mapping

### S07 — ModelBuilder Overloads

**Pattern:** `ApplyBaseEntityConfiguration<TKey>()`, `ApplyBaseAuditableEntityConfiguration<TUserKey>()`

Demonstrates the generic `ModelBuilder` extension overloads:

- `ApplyBaseAuditableEntityConfiguration<Guid>()` — configures all `IBaseAuditableEntity<Guid>` entities
- `ApplyBaseEntityConfiguration<int>()` and `ApplyBaseEntityConfiguration<Guid>()` — configures all `IBaseEntity<int>` and `IBaseEntity<Guid>` entities
- Shows separate DbContext specifically for testing these overloads

## Project structure

```
DotEmilu.Samples.EntityFrameworkCore/
├── Program.cs                         # Runs S01-S07 sequentially
├── Entities/
│   └── Song.cs                        # BaseEntity<Guid> with SongType enum
├── Mocks/
│   └── MockContextUser.cs             # IAppUserContext implementation
├── DataAccess/
│   ├── InvoiceDbContext.cs            # Main DbContext with OnModelCreating
│   ├── Configurations/
│   │   ├── InvoiceConfiguration.cs    # Invoice model config
│   │   └── SongConfiguration.cs       # Song model config with UseIsDeleted, HasFormattedComment
│   └── CqrsPattern/
│       ├── IEntities.cs               # Shared DbSet definitions
│       ├── ICommands.cs               # IEntities + IUnitOfWork
│       ├── IQueries.cs                # IEntities (read-only)
│       ├── Commands.cs                # Tracked implementation
│       └── Queries.cs                 # No-tracking implementation
├── Scenarios/
│   ├── IScenario.cs
│   ├── Print.cs
│   ├── S01AuditableExplicit/
│   ├── S02AuditableReflection/
│   ├── S03SoftDelete/
│   ├── S04PaginatedList/
│   ├── S05CqrsExecution/
│   ├── S06ModelConfiguration/
│   └── S07ModelBuilderOverloads/
```
