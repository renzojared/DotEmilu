# DotEmilu

A .NET 10 library ecosystem for building **use-case-driven applications** with validation-first handler pipelines, chain-of-responsibility workflows, ASP.NET Core Minimal API integration, and Entity Framework Core utilities.

[![NuGet](https://img.shields.io/nuget/v/DotEmilu.svg)](https://www.nuget.org/packages/DotEmilu)
[![codecov](https://codecov.io/gh/renzojared/DotEmilu/graph/badge.svg?token=10VJ0H5X5S)](https://codecov.io/gh/renzojared/DotEmilu)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Packages

| Package | Description | NuGet |
|---------|-------------|-------|
| [DotEmilu.Abstractions](src/DotEmilu.Abstractions) | Core interfaces and base classes — `IHandler`, `IRequest`, `IVerifier`, `BaseEntity`, `BaseAuditableEntity`, `PaginatedList` | [![NuGet](https://img.shields.io/nuget/v/DotEmilu.Abstractions.svg)](https://www.nuget.org/packages/DotEmilu.Abstractions) |
| [DotEmilu](src/DotEmilu) | Handler pipeline — `Handler<TRequest>`, `Handler<TRequest, TResponse>`, `ChainHandler<TChain>`, FluentValidation integration, DI registration | [![NuGet](https://img.shields.io/nuget/v/DotEmilu.svg)](https://www.nuget.org/packages/DotEmilu) |
| [DotEmilu.AspNetCore](src/DotEmilu.AspNetCore) | ASP.NET Core bridge — `HttpHandler`, `AsDelegate`, `IPresenter`, RFC 7807 Problem Details | [![NuGet](https://img.shields.io/nuget/v/DotEmilu.AspNetCore.svg)](https://www.nuget.org/packages/DotEmilu.AspNetCore) |
| [DotEmilu.EntityFrameworkCore](src/DotEmilu.EntityFrameworkCore) | EF Core utilities — auditable entities, soft delete, base configurations, mapping strategies, pagination | [![NuGet](https://img.shields.io/nuget/v/DotEmilu.EntityFrameworkCore.svg)](https://www.nuget.org/packages/DotEmilu.EntityFrameworkCore) |

### Dependency chain

```
DotEmilu.Abstractions          (standalone, base contracts)
    │
    ├── DotEmilu               (handler pipeline + validation)
    │       │
    │       └── DotEmilu.AspNetCore   (HTTP bridge + Minimal API)
    │
    └── DotEmilu.EntityFrameworkCore  (EF Core utilities)
```

## Project structure

```
DotEmilu/
├── src/
│   ├── DotEmilu.Abstractions/         # Core interfaces: IHandler, IRequest, IVerifier, BaseEntity
│   ├── DotEmilu/                      # Handler pipeline: Handler<T>, ChainHandler<T>, Verifier<T>
│   ├── DotEmilu.AspNetCore/           # HTTP bridge: HttpHandler, AsDelegate, IPresenter
│   └── DotEmilu.EntityFrameworkCore/  # EF Core: interceptors, configurations, pagination, CQRS
├── samples/
│   ├── DotEmilu.Samples.Domain/       # Shared entities and request contracts
│   ├── DotEmilu.Samples.ConsoleApp/   # 8 console scenarios (S01–S08)
│   ├── DotEmilu.Samples.EntityFrameworkCore/  # 7 EF Core scenarios (S01–S07)
│   └── DotEmilu.Samples.FullApp/      # Complete ASP.NET Core Minimal API
├── tests/
│   ├── DotEmilu.UnitTests/
│   └── DotEmilu.IntegrationTests/
└── docs/
    ├── getting-started.md
    └── guides/
        ├── handler-patterns.md
        ├── entity-framework-core-patterns.md
        └── coverage-configuration.md
```

## Quick start

### 1. Install

```bash
# For a Minimal API project (includes core + abstractions transitively)
dotnet add package DotEmilu.AspNetCore

# For EF Core utilities
dotnet add package DotEmilu.EntityFrameworkCore
```

### 2. Define a request, validator, and handler

```csharp
// FILE: samples/DotEmilu.Samples.Domain/Contracts/CreateInvoiceRequest.cs
public record CreateInvoiceRequest(
    string Number,
    string Description,
    decimal Amount,
    DateOnly Date) : IRequest;
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/Validator.cs
internal sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Date).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));
    }
}
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/CreateInvoice.cs
internal sealed class Handler(IVerifier<CreateInvoiceRequest> verifier, IInvoiceCommands db)
    : Handler<CreateInvoiceRequest>(verifier)
{
    protected override async Task HandleUseCaseAsync(
        CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        db.Invoices.Add(new Invoice
        {
            Number = request.Number,
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

### 3. Wire the endpoint

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/CreateInvoice.cs
builder
    .MapPost(string.Empty, AsDelegate.ForAsync<CreateInvoiceRequest>(TypedResults.Created))
    .WithName("CreateInvoice");
```

### 4. Register services

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/FeaturesContainer.cs
services
    .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true)
    .AddVerifier()
    .AddHandlers(Assembly.GetExecutingAssembly())
    .AddChainHandlers(Assembly.GetExecutingAssembly());
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/WebApiContainer.cs
services
    .AddDotEmilu()   // HttpHandler + Presenter + Verifier
    .AddOpenApi();
```

### 5. Configure response messages

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

## Guides

For detailed patterns and advanced usage, see the full documentation:

- [Getting started](docs/getting-started.md) — prerequisites, installation options, step-by-step tutorials
- [Handler patterns](docs/guides/handler-patterns.md) — all handler types, lifecycle hooks, chain-of-responsibility, orchestrating handlers
- [Entity Framework Core patterns](docs/guides/entity-framework-core-patterns.md) — auditable entities, soft delete, CQRS, pagination, mapping strategies
- [Coverage configuration](docs/guides/coverage-configuration.md) — coverlet.MTP CLI flags, codecov.yml settings, CI pipeline coverage setup

## Samples

Runnable projects demonstrating every pattern:

| Sample | Description |
|--------|-------------|
| [DotEmilu.Samples.ConsoleApp](samples/DotEmilu.Samples.ConsoleApp) | 8 self-contained console scenarios covering all handler patterns |
| [DotEmilu.Samples.Domain](samples/DotEmilu.Samples.Domain) | Shared entity definitions and request contracts |
| [DotEmilu.Samples.EntityFrameworkCore](samples/DotEmilu.Samples.EntityFrameworkCore) | 7 scenarios for interceptors, configurations, CQRS, pagination |
| [DotEmilu.Samples.FullApp](samples/DotEmilu.Samples.FullApp) | Complete ASP.NET Core Minimal API with all patterns integrated |

## License

[MIT](LICENSE)
