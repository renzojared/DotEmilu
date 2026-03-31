# DotEmilu.Samples.FullApp

Complete ASP.NET Core Minimal API demonstrating all DotEmilu patterns working together: handler pipeline, chain-of-responsibility, orchestrating handlers, CQRS, auditable entities, soft delete, pagination, and background workers.

## Run

```bash
cd samples/DotEmilu.Samples.FullApp
dotnet run
```

Uses an in-memory SQLite database. OpenAPI is available in Development mode. A demo user is configured in `CurrentUser` for audit tracking.

## Endpoints

| Method | Route | Pattern | Description |
|--------|-------|---------|-------------|
| `POST` | `/invoices` | `Handler<TRequest>` | Create a new invoice |
| `GET` | `/invoices` | `Handler<TRequest, TResponse>` + `AsPaginatedListAsync` | Get paginated list of invoices |
| `GET` | `/invoices/{id}` | `Handler<TRequest, TResponse>` + custom `AsDelegate` result | Get invoice by ID (returns 404 if not found) |
| `PUT` | `/invoices/{id}` | `Handler<TRequest>` + manual validation | Update an invoice (runtime not-found check) |
| `DELETE` | `/invoices/{id}` | `Handler<TRequest>` + soft delete | Soft-delete via `db.Remove()` + interceptors |
| `POST` | `/invoices/{id}/confirm` | **Orchestrating handler** | Delegates to `ValidateInvoiceForConfirmationHandler`, propagates errors via `IVerifier` |
| `POST` | `/invoices/sync` | **Chain handler pipeline** | Validate → Load → Apply chain via `SyncInvoicesProcessor` |

## Key patterns demonstrated

### CQRS

Interface segregation separates read and write concerns:

- `IInvoiceCommands` — tracked DbContext + `IUnitOfWork` (for write handlers)
- `IInvoiceQueries` — no-tracking DbContext (for read handlers)

### Orchestrating handler (Confirm Invoice)

`ConfirmInvoiceHandler` composes `ValidateInvoiceForConfirmationHandler`:
1. Calls sub-handler to check if invoice exists and is not already paid
2. Checks sub-handler's `IVerifier` for errors
3. Propagates errors via `_verifier.AddValidationErrors()`
4. On success, marks invoice as paid

### Chain handler pipeline (Sync Invoices)

`SyncInvoicesProcessor` wires three `ChainHandler<SyncInvoicesContext>` steps:
1. `ValidateSyncPreconditionsHandler` — validates non-empty IDs, max 100
2. `LoadTargetInvoicesHandler` — loads invoices from database
3. `ApplySyncHandler` — applies sync timestamp and persists

### Auditable entities + Soft delete

- `AuditableEntityInterceptor<Guid>` auto-populates `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`, `Deleted`, `DeletedBy`
- `SoftDeleteInterceptor` converts hard deletes to soft deletes
- `CurrentUser` resolves the current user from HTTP context claims (with fallback demo user)

### Background worker

`InvoiceNotificationWorker` is a `BackgroundService` that:
- Creates a DI scope via `IServiceScopeFactory`
- Resolves a scoped handler (`IHandler<GetInvoicesRequest, PaginatedList<Invoice>>`)
- Periodically queries invoices to simulate notifications

### Endpoint wiring with AsDelegate

All endpoints use `AsDelegate.ForAsync<TRequest>()` for zero-boilerplate Minimal API registration.

## DI containers

Services are registered in organized containers:

| Container | Registers |
|-----------|-----------|
| `WebApiContainer` | `AddDotEmilu()`, `AddOpenApi()` |
| `FeaturesContainer` | Validators, Verifier, Handlers, ChainHandlers |
| `DataAccessContainer` | Interceptors, `CurrentUser`, DbContext, `IInvoiceCommands`, `IInvoiceQueries` |

## Project structure

```
DotEmilu.Samples.FullApp/
├── Program.cs                              # WebApplication setup
├── DiContainers/
│   ├── WebApiContainer.cs                  # AddDotEmilu + OpenApi
│   ├── FeaturesContainer.cs                # Handlers + Validators
│   └── DataAccessContainer.cs              # EF Core + Interceptors
├── Features/
│   ├── InvoiceEndpoints.cs                 # MapGroup("/invoices") route definitions
│   ├── CreateInvoice/
│   │   ├── CreateInvoice.cs                # Handler + endpoint
│   │   └── Validator.cs
│   ├── GetInvoices/
│   ├── GetInvoiceById/
│   ├── UpdateInvoice/
│   ├── DeleteInvoice/
│   ├── ConfirmInvoice/
│   │   ├── ConfirmInvoice.cs               # Orchestrating handler
│   │   ├── ValidateInvoiceForConfirmationHandler.cs  # Sub-handler
│   │   └── Validator.cs
│   └── SyncInvoices/
│       ├── SyncInvoices.cs                 # Main handler
│       ├── SyncInvoicesContext.cs           # Chain context
│       ├── SyncInvoicesProcessor.cs         # Chain wiring
│       ├── ValidateSyncPreconditionsHandler.cs
│       ├── LoadTargetInvoicesHandler.cs
│       ├── ApplySyncHandler.cs
│       └── Validator.cs
├── Infrastructure/
│   ├── Auth/
│   │   └── CurrentUser.cs                  # IAppUserContext from HttpContext claims
│   ├── Configurations/
│   │   └── InvoiceConfiguration.cs         # EF model config
│   └── Persistence/
│       ├── InvoiceDbContext.cs              # DbContext with audit + base entity config
│       ├── CqrsInterfaces.cs               # IInvoiceEntities, IInvoiceCommands, IInvoiceQueries
│       └── CqrsContexts.cs                 # InvoiceCommands, InvoiceQueries
└── Workers/
    └── InvoiceNotificationWorker.cs        # BackgroundService using scoped handlers
```

## Smoke test

Scripts in `scripts/` exercise all endpoints end-to-end. Start the API first:

```bash
dotnet run --project samples/DotEmilu.Samples.FullApp
```

Then in another terminal:

```bash
# macOS / Linux
bash samples/DotEmilu.Samples.FullApp/scripts/smoke.sh

# Windows (PowerShell)
powershell -ExecutionPolicy Bypass -File samples/DotEmilu.Samples.FullApp/scripts/smoke.ps1
```

To override the base URL:

```bash
# macOS / Linux
BASE_URL=http://localhost:5009 bash samples/DotEmilu.Samples.FullApp/scripts/smoke.sh

# Windows (PowerShell)
$env:BASE_URL = "http://localhost:5009"
powershell -ExecutionPolicy Bypass -File samples/DotEmilu.Samples.FullApp/scripts/smoke.ps1
```

The script runs 10 steps in order:

1. Create invoice
2. List invoices (paginated)
3. Get invoice by id
4. Confirm invoice (orchestrating handler)
5. Get confirmed invoice (verify `IsPaid = true`)
6. Update invoice
7. Get updated invoice
8. Sync invoices (chain handler pipeline)
9. Get synced invoice
10. Delete invoice (soft-delete) + verify it returns 404 and disappears from the list
