# DotEmilu

[![NuGet](https://img.shields.io/nuget/v/DotEmilu.svg)](https://www.nuget.org/packages/DotEmilu)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)

Use-case handler pipeline for .NET — validation-first handlers, chain-of-responsibility workflows, lifecycle hooks, and assembly-based DI registration powered by FluentValidation.

## Install

```bash
dotnet add package DotEmilu
```

## What's included

### Handler base classes

| Type | Purpose |
|------|---------|
| `Handler<TRequest>` | Handles a request with no response. Validates via `IVerifier<TRequest>` before calling `HandleUseCaseAsync`. |
| `Handler<TRequest, TResponse>` | Same, but returns a typed response. Returns `default` if validation fails. |
| `BaseHandler` | Abstract base providing `HandlingAsync()` with try/catch/finally, `HandleExceptionAsync()`, and `FinalizeAsync()` lifecycle hooks. |
| `ChainHandler<TChain>` | Chain-of-responsibility node. Call `SetSuccessor()` to link, then `ContinueAsync()` to execute. |

### Validation

| Type | Purpose |
|------|---------|
| `Verifier<TRequest>` (internal) | Aggregates all registered `IValidator<TRequest>` instances, runs them in parallel, and collects errors. |

### DI registration

| Extension method | Purpose |
|-----------------|---------|
| `services.AddVerifier()` | Registers `IVerifier<>` → `Verifier<>` as scoped |
| `services.AddHandlers(assembly)` | Scans an assembly for `IHandler<>` and `IHandler<,>` implementations and registers them as scoped |
| `services.AddChainHandlers(assembly)` | Scans for `ChainHandler<>` subclasses and registers them as scoped |

## Handler with no response

```csharp
public record CreateInvoiceRequest(string Number, decimal Amount, DateOnly Date) : IRequest;

public class CreateInvoiceHandler(IVerifier<CreateInvoiceRequest> verifier, IInvoiceCommands db)
    : Handler<CreateInvoiceRequest>(verifier)
{
    protected override async Task HandleUseCaseAsync(
        CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        db.Invoices.Add(new Invoice
        {
            Number = request.Number,
            Amount = request.Amount,
            Date = request.Date
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

## Handler with response

```csharp
public record GetInvoiceByIdRequest(int Id) : IRequest<InvoiceResponse>;
public record InvoiceResponse(int Id, string Number, string Description, decimal Amount);

public class GetInvoiceByIdHandler(IVerifier<GetInvoiceByIdRequest> verifier, IInvoiceQueries db)
    : Handler<GetInvoiceByIdRequest, InvoiceResponse>(verifier)
{
    protected override async Task<InvoiceResponse?> HandleUseCaseAsync(
        GetInvoiceByIdRequest request, CancellationToken cancellationToken)
    {
        return await db.Invoices
            .Where(i => i.Id == request.Id)
            .Select(i => new InvoiceResponse(i.Id, i.Number, i.Description, i.Amount))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

## Lifecycle hooks

Every handler inherits `HandleExceptionAsync` and `FinalizeAsync` from `BaseHandler`:

```csharp
public class ProcessPaymentHandler(IVerifier<ProcessPaymentRequest> verifier)
    : Handler<ProcessPaymentRequest>(verifier)
{
    protected override Task HandleUseCaseAsync(ProcessPaymentRequest request, CancellationToken ct)
    {
        // core payment logic
        return Task.CompletedTask;
    }

    protected override Task HandleExceptionAsync(Exception e)
    {
        // runs on exception (before re-throw) — logging, compensation, etc.
        return Task.CompletedTask;
    }

    protected override Task FinalizeAsync()
    {
        // runs always (like finally) — cleanup resources
        return Task.CompletedTask;
    }
}
```

## Manual validation errors

Inject and use `IVerifier` directly inside your handler to add runtime validation errors:

```csharp
public class LoginHandler(
    IVerifier<LoginRequest> verifier,
    IUserRepository userRepo)
    : Handler<LoginRequest, LoginResult>(verifier)
{
    private readonly IVerifier<LoginRequest> _verifier = verifier;

    protected override async Task<LoginResult?> HandleUseCaseAsync(
        LoginRequest request, CancellationToken ct)
    {
        var user = await userRepo.FindByEmailAsync(request.Email, ct);
        if (user is null)
        {
            _verifier.AddValidationError("Credentials", "Invalid email or password.");
            return null;
        }
        return new LoginResult(user.Id, user.Name);
    }
}
```

## Chain of responsibility

```csharp
// Step handlers
public class ValidateStep : ChainHandler<SyncContext>
{
    public override async Task ContinueAsync(SyncContext chain, CancellationToken ct)
    {
        // validation logic...
        if (Successor is not null)
            await Successor.ContinueAsync(chain, ct);
    }
}

// Wire the chain in a processor
public class SyncProcessor : IHandler<SyncContext>
{
    private readonly ValidateStep _first;

    public SyncProcessor(ValidateStep validate, EnrichStep enrich, PersistStep persist)
    {
        validate.SetSuccessor(enrich).SetSuccessor(persist);
        _first = validate;
    }

    public Task HandleAsync(SyncContext context, CancellationToken ct)
        => _first.ContinueAsync(context, ct);
}
```

## DI registration

```csharp
services
    .AddVerifier()                                    // IVerifier<> → Verifier<>
    .AddHandlers(Assembly.GetExecutingAssembly())     // scan IHandler<> and IHandler<,>
    .AddChainHandlers(Assembly.GetExecutingAssembly()) // scan ChainHandler<>
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
| **DotEmilu** *(this)* | Handler pipeline with FluentValidation |
| [DotEmilu.AspNetCore](https://www.nuget.org/packages/DotEmilu.AspNetCore) | ASP.NET Core Minimal API integration |
| [DotEmilu.EntityFrameworkCore](https://www.nuget.org/packages/DotEmilu.EntityFrameworkCore) | EF Core interceptors and configurations |

## Feedback

File bugs, feature requests, or questions on [GitHub Issues](https://github.com/renzojared/DotEmilu/issues).

[Full documentation & source](https://github.com/renzojared/DotEmilu)
