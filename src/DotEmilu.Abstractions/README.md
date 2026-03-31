# DotEmilu.Abstractions

[![NuGet](https://img.shields.io/nuget/v/DotEmilu.Abstractions.svg)](https://www.nuget.org/packages/DotEmilu.Abstractions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Core interfaces and base classes for the **DotEmilu** ecosystem. This package has no dependency on DI containers or ASP.NET Core — it defines the contracts that all other DotEmilu packages build upon.

## Install

```bash
dotnet add package DotEmilu.Abstractions
```

## What's included

### Handler contracts

| Interface | Purpose |
|-----------|---------|
| `IHandler<TRequest>` | Handles a request with no return value |
| `IHandler<TRequest, TResponse>` | Handles a request and returns a typed response |
| `IHandler` | Parameterless handler (e.g., seed data, scheduled tasks) |

### Request contracts

| Interface | Purpose |
|-----------|---------|
| `IRequest` | Marker for requests with no response |
| `IRequest<TResponse>` | Marker for requests that produce a response |
| `IBaseRequest` | Base marker interface for all requests |

### Validation

| Type | Purpose |
|------|---------|
| `IVerifier<TRequest>` | Validates a request and accumulates `ValidationFailure` errors |
| `IVerifier` | Base interface exposing `ValidationErrors`, `IsValid`, `AddValidationError()`, `AddValidationErrors()` |

### Base entities

| Type | Purpose |
|------|---------|
| `IBaseEntity` | Soft-delete contract (`IsDeleted`) |
| `IBaseEntity<TKey>` | Adds a struct-typed `Id` primary key |
| `BaseEntity<TKey>` | Abstract class implementing `IBaseEntity<TKey>` |
| `IBaseAuditableEntity<TUserKey>` | Audit fields: `Created`, `CreatedBy`, `LastModified`, `LastModifiedBy`, `Deleted`, `DeletedBy` |
| `IBaseAuditableEntity<TKey, TUserKey>` | Combines `IBaseEntity<TKey>` + `IBaseAuditableEntity<TUserKey>` |
| `BaseAuditableEntity<TUserKey>` | Abstract class with audit fields (no primary key) |
| `BaseAuditableEntity<TKey, TUserKey>` | Abstract class with primary key + audit fields |

### Utilities

| Type | Purpose |
|------|---------|
| `PaginatedList<T>` | Paginated collection with `Items`, `PageNumber`, `TotalPages`, `TotalCount`, `HasPreviousPage`, `HasNextPage` |
| `IContextUser<TUserKey>` | Contract to expose the current user's `Id` (used by auditable interceptors) |

## Usage

```csharp
// Define a request with no response
public record CreateInvoiceRequest(string Number, decimal Amount) : IRequest;

// Define a request with a typed response
public record GetInvoiceByIdRequest(int Id) : IRequest<InvoiceResponse>;
public record InvoiceResponse(int Id, string Number, decimal Amount);

// Define an auditable entity
public class Invoice : BaseAuditableEntity<int, Guid>
{
    public string Number { get; set; } = default!;
    public decimal Amount { get; set; }
}

// Expose current user identity
public class CurrentUser(IHttpContextAccessor accessor) : IContextUser<Guid>
{
    public Guid Id => Guid.Parse(accessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

## Additional documentation

- [Getting started](https://github.com/renzojared/DotEmilu/blob/main/docs/getting-started.md)
- [Handler patterns](https://github.com/renzojared/DotEmilu/blob/main/docs/guides/handler-patterns.md)
- [Entity Framework Core patterns](https://github.com/renzojared/DotEmilu/blob/main/docs/guides/entity-framework-core-patterns.md)

## Part of the DotEmilu ecosystem

| Package | Description |
|---------|-------------|
| **DotEmilu.Abstractions** *(this)* | Core interfaces and base classes |
| [DotEmilu](https://www.nuget.org/packages/DotEmilu) | Handler pipeline with FluentValidation |
| [DotEmilu.AspNetCore](https://www.nuget.org/packages/DotEmilu.AspNetCore) | ASP.NET Core Minimal API integration |
| [DotEmilu.EntityFrameworkCore](https://www.nuget.org/packages/DotEmilu.EntityFrameworkCore) | EF Core interceptors and configurations |

## Feedback

File bugs, feature requests, or questions on [GitHub Issues](https://github.com/renzojared/DotEmilu/issues).

[Full documentation & source](https://github.com/renzojared/DotEmilu)
