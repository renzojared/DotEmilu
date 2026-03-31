# DotEmilu.Samples.Domain

Shared domain layer used by all other sample projects. Contains entity definitions, request contracts, and the `IAppUserContext` interface.

## Entities

### Invoice

```csharp
public class Invoice : BaseAuditableEntity<int, Guid>
{
    public string Number { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public bool IsPaid { get; set; }
}
```

Full auditable entity with:
- `int` primary key (`Id`)
- `Guid` user key for audit fields (`CreatedBy`, `LastModifiedBy`, `DeletedBy`)
- Soft-delete support (`IsDeleted`)

## Request contracts

| Request | Type | Description |
|---------|------|-------------|
| `CreateInvoiceRequest` | `IRequest` | Create a new invoice (Number, Description, Amount, Date) |
| `CreateInvoiceWithConfirmationRequest` | `IRequest<string>` | Create and return a confirmation string |
| `GetInvoiceByIdRequest` | `IRequest<InvoiceResponse>` | Get a single invoice by ID |
| `GetInvoicesRequest` | `IRequest<PaginatedList<Invoice>>` | Get paginated list of invoices |
| `UpdateInvoiceRequest` | `IRequest` | Update an existing invoice |
| `DeleteInvoiceRequest` | `IRequest` | Soft-delete an invoice |
| `ConfirmInvoiceRequest` | `IRequest<ConfirmInvoiceResponse>` | Confirm/pay an invoice (orchestrating pattern) |
| `ValidateInvoiceForConfirmationRequest` | `IRequest` | Sub-handler validation for confirm flow |

## Interfaces

| Interface | Description |
|-----------|-------------|
| `IAppUserContext` | Extends `IContextUser<Guid>` — marker for the application's user context implementation |

## Used by

- [DotEmilu.Samples.ConsoleApp](../DotEmilu.Samples.ConsoleApp) — uses `CreateInvoiceRequest` and `CreateInvoiceWithConfirmationRequest`
- [DotEmilu.Samples.EntityFrameworkCore](../DotEmilu.Samples.EntityFrameworkCore) — uses `Invoice` entity and EF configurations
- [DotEmilu.Samples.FullApp](../DotEmilu.Samples.FullApp) — uses all contracts and entities for the full Web API
