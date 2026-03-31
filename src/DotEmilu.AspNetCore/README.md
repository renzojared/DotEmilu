# DotEmilu.AspNetCore

[![NuGet](https://img.shields.io/nuget/v/DotEmilu.AspNetCore.svg)](https://www.nuget.org/packages/DotEmilu.AspNetCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

ASP.NET Core integration for **DotEmilu** — bridges use-case handlers to Minimal API endpoints, formats errors as RFC 7807 Problem Details, and provides `AsDelegate` for zero-boilerplate endpoint wiring.

## Install

```bash
dotnet add package DotEmilu.AspNetCore
```

This transitively installs `DotEmilu` and `DotEmilu.Abstractions`.

## What's included

### HTTP handlers

| Type | Purpose |
|------|---------|
| `HttpHandler<TRequest>` | Wraps `IHandler<TRequest>` — executes handler, checks validation, returns `IResult` |
| `HttpHandler<TRequest, TResponse>` | Wraps `IHandler<TRequest, TResponse>` — same, but with a typed response payload |

### Delegate helpers

| Type | Purpose |
|------|---------|
| `AsDelegate.ForAsync<TRequest>()` | Converts a handler into a `Func<TRequest, HttpHandler, CancellationToken, Task<IResult>>` for Minimal API |
| `AsDelegate.ForAsync<TRequest, TResponse>()` | Same, for handlers with a response |

### Presenter

| Type | Purpose |
|------|---------|
| `IPresenter` | Interface for formatting HTTP responses (`Success`, `ValidationError`, `ServerError`) |
| `Presenter` (internal) | Default implementation: returns `ValidationProblem` (400) and `Problem` (500) using RFC 7807, with dev-mode stack traces |

### Configuration

| Type | Purpose |
|------|---------|
| `ResultMessage` | Options class bound to `appsettings.json` section `"ResultMessage"` — defines titles and details for validation/server errors |

### DI registration

| Method | Purpose |
|--------|---------|
| `services.AddDotEmilu()` | Registers `IVerifier<>`, `HttpHandler<>`, `HttpHandler<,>`, `IPresenter`, and binds `ResultMessage` from configuration |

## Endpoint wiring with AsDelegate

The cleanest way to define endpoints — no handler variable, no manual `HandleAsync` call:

```csharp
var invoices = app.MapGroup("/invoices");

// POST /invoices → 201 Created
invoices.MapPost("", AsDelegate.ForAsync<CreateInvoiceRequest>(TypedResults.Created));

// GET /invoices → 200 OK with PaginatedList<Invoice>
invoices.MapGet("", AsDelegate.ForAsync<GetInvoicesRequest, PaginatedList<Invoice>>());

// GET /invoices/{id} → 200 OK or 404 Not Found
invoices.MapGet("{id:int}", AsDelegate.ForAsync<GetInvoiceByIdRequest, InvoiceResponse>(
    response => response is null ? TypedResults.NotFound() : TypedResults.Ok(response)));

// PUT /invoices/{id} → 204 No Content
invoices.MapPut("{id:int}", AsDelegate.ForAsync<UpdateInvoiceRequest>(TypedResults.NoContent));

// DELETE /invoices/{id} → 204 No Content
invoices.MapDelete("{id:int}", AsDelegate.ForAsync<DeleteInvoiceRequest>(TypedResults.NoContent));
```

## Manual HttpHandler usage

If you need more control, inject `HttpHandler` directly:

```csharp
app.MapPost("/invoices", async (
    CreateInvoiceRequest request,
    HttpHandler<CreateInvoiceRequest> handler,
    CancellationToken ct) =>
        await handler.HandleAsync(request, ct, () => TypedResults.Created()));
```

## Configuration

Add to `appsettings.json`:

```json
{
  "ResultMessage": {
    "ValidationError": {
      "Title": "Bad Request",
      "Detail": "One or more validation errors occurred."
    },
    "ServerError": {
      "Title": "Internal Server Error",
      "Detail": "An unexpected error occurred. Please contact the administrator."
    }
  }
}
```

## Full registration

```csharp
builder.Services
    .AddDotEmilu()                                           // HttpHandler + Presenter + Verifier
    .AddHandlers(Assembly.GetExecutingAssembly())            // scan handlers
    .AddChainHandlers(Assembly.GetExecutingAssembly())       // scan chain handlers
    .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); // FluentValidation validators
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
| **DotEmilu.AspNetCore** *(this)* | ASP.NET Core Minimal API integration |
| [DotEmilu.EntityFrameworkCore](https://www.nuget.org/packages/DotEmilu.EntityFrameworkCore) | EF Core interceptors and configurations |

## Feedback

File bugs, feature requests, or questions on [GitHub Issues](https://github.com/renzojared/DotEmilu/issues).

[Full documentation & source](https://github.com/renzojared/DotEmilu)
