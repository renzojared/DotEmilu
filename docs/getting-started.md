---
title: "Getting Started"
description: "Prerequisites, installation options, and step-by-step tutorials for DotEmilu — a .NET 10 handler pipeline ecosystem."
---

# Getting started

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

## Installation

### Via NuGet (stable releases)

```bash
# For ASP.NET Core Web API projects (includes DotEmilu + Abstractions transitively)
dotnet add package DotEmilu.AspNetCore

# For EF Core utilities (includes Abstractions transitively)
dotnet add package DotEmilu.EntityFrameworkCore

# Core handler pipeline only (includes Abstractions transitively)
dotnet add package DotEmilu

# Abstractions only (interfaces, base entities, contracts)
dotnet add package DotEmilu.Abstractions
```

### Pre-release versions

```bash
dotnet add package DotEmilu.AspNetCore --prerelease
dotnet add package DotEmilu.EntityFrameworkCore --prerelease
```

### Via GitHub Package Registry

Add the GitHub source to your `nuget.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github-dotemilu" value="https://nuget.pkg.github.com/renzojared/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github-dotemilu>
      <add key="Username" value="YOUR_GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="YOUR_GITHUB_PAT" />
    </github-dotemilu>
  </packageSourceCredentials>
</configuration>
```

Then install normally:

```bash
dotnet add package DotEmilu.AspNetCore --source github-dotemilu
```

## Package comparison

| Package | Install when you need… |
|---------|----------------------|
| `DotEmilu.Abstractions` | Only the interfaces (`IHandler`, `IRequest`, `IVerifier`) and base entities (`BaseEntity`, `BaseAuditableEntity`). Use this in domain/shared projects that shouldn't depend on DI or ASP.NET Core. |
| `DotEmilu` | The full handler pipeline (`Handler<TRequest>`, `Handler<TRequest, TResponse>`, `ChainHandler<TChain>`) with FluentValidation and DI registration. Use in application/use-case layers. |
| `DotEmilu.AspNetCore` | Everything above + `HttpHandler`, `AsDelegate`, `IPresenter` for wiring handlers to Minimal API endpoints with RFC 7807 Problem Details. Use in ASP.NET Core Web API projects. |
| `DotEmilu.EntityFrameworkCore` | Auditable entity interceptors, soft-delete interceptor, base entity configurations, mapping strategies, pagination. Independent from the handler pipeline — depends only on Abstractions. |

### Dependency chain

```
DotEmilu.Abstractions            ← standalone, no external DI
    │
    ├── DotEmilu                 ← adds FluentValidation, MS DI
    │       │
    │       └── DotEmilu.AspNetCore    ← adds ASP.NET Core framework
    │
    └── DotEmilu.EntityFrameworkCore   ← adds EF Core Relational
```

## Minimal Web API — step by step

This tutorial follows the [FullApp sample](../samples/DotEmilu.Samples.FullApp).

### 1. Create the project and install packages

```bash
dotnet new webapi -n MyApp
cd MyApp
dotnet add package DotEmilu.AspNetCore
dotnet add package DotEmilu.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.InMemory  # or your provider
```

### 2. Define a domain entity

```csharp
// FILE: samples/DotEmilu.Samples.Domain/Entities/Invoice.cs
public class Invoice : BaseAuditableEntity<int, Guid>
{
    public string Number { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public bool IsPaid { get; set; }
}
```

### 3. Define a request contract

```csharp
// FILE: samples/DotEmilu.Samples.Domain/Contracts/CreateInvoiceRequest.cs
public record CreateInvoiceRequest(
    string Number,
    string Description,
    decimal Amount,
    DateOnly Date) : IRequest;
```

### 4. Add validation rules

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/Validator.cs
internal sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Invoice number is required.")
            .MaximumLength(20).WithMessage("Invoice number cannot exceed 20 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Date)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Date cannot be in the future.");
    }
}
```

### 5. Implement the handler

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/CreateInvoice.cs
internal sealed class Handler(IVerifier<CreateInvoiceRequest> verifier, IInvoiceCommands db)
    : Handler<CreateInvoiceRequest>(verifier)
{
    protected override async Task HandleUseCaseAsync(
        CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = new Invoice
        {
            Number = request.Number,
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

### 6. Wire the endpoint

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/CreateInvoice.cs
builder
    .MapPost(string.Empty, AsDelegate.ForAsync<CreateInvoiceRequest>(TypedResults.Created))
    .WithName("CreateInvoice")
    .WithSummary("Creates a new invoice")
    .Produces(StatusCodes.Status201Created)
    .ProducesValidationProblem()
    .ProducesProblem(StatusCodes.Status500InternalServerError);
```

### 7. Register services

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/WebApiContainer.cs
services
    .AddDotEmilu()   // HttpHandler + Presenter + Verifier
    .AddOpenApi();
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/FeaturesContainer.cs
services
    .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true)
    .AddVerifier()
    .AddHandlers(Assembly.GetExecutingAssembly())
    .AddChainHandlers(Assembly.GetExecutingAssembly());
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/DataAccessContainer.cs
services
    .AddSoftDeleteInterceptor()
    .AddAuditableEntityInterceptors(Assembly.GetExecutingAssembly());

services.AddDbContext<InvoiceDbContext>((sp, options) =>
    options
        .UseInMemoryDatabase("FullAppInvoicesDb")
        .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));

services.AddScoped<IInvoiceCommands, InvoiceCommands>();
services.AddScoped<IInvoiceQueries, InvoiceQueries>();
```

### 8. Configure error messages

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

### 9. Build the application

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddFeatures()
    .AddDataAccess()
    .AddWebApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.MapInvoiceEndpoints();

await app.RunAsync();
```

## Console app — step by step

This tutorial follows the [ConsoleApp sample](../samples/DotEmilu.Samples.ConsoleApp) (Scenario S01).

### 1. Create the project

```bash
dotnet new console -n MyConsoleApp
cd MyConsoleApp
dotnet add package DotEmilu
```

### 2. Build the DI container

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S01BasicHandler/Container.cs
var services = new ServiceCollection();
var assembly = Assembly.GetExecutingAssembly();

services
    .AddValidatorsFromAssembly(assembly, includeInternalTypes: true)
    .AddVerifier()
    .AddHandlers(assembly);

var provider = services.BuildServiceProvider();
```

### 3. Resolve and run a handler

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S01BasicHandler/Scenario.cs
await using var provider = Container.Build();
await using var scope = provider.CreateAsyncScope();

var handler = scope.ServiceProvider.GetRequiredService<IHandler<CreateInvoiceRequest>>();
var verifier = scope.ServiceProvider.GetRequiredService<IVerifier<CreateInvoiceRequest>>();

var request = new CreateInvoiceRequest(
    Number: "INV-001",
    Description: "Consulting services",
    Amount: 1500m,
    Date: DateOnly.FromDateTime(DateTime.Today));

await handler.HandleAsync(request, CancellationToken.None);

if (!verifier.IsValid)
{
    foreach (var error in verifier.ValidationErrors)
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
}
```

## Next steps

- [Handler patterns](guides/handler-patterns.md) — all handler types, lifecycle hooks, chain-of-responsibility, orchestrating handlers
- [Entity Framework Core patterns](guides/entity-framework-core-patterns.md) — auditable entities, soft delete, CQRS, pagination, mapping strategies
- [Samples](../samples/) — runnable projects demonstrating every pattern
