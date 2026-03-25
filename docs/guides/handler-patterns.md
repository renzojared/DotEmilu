---
title: "Handler Patterns"
description: "Complete guide to DotEmilu handler types: fire-and-forget, response handlers, parameterless handlers, lifecycle hooks, manual validation, chain-of-responsibility, and orchestrating handlers."
---

# Handler patterns

This guide covers every handler pattern in the DotEmilu pipeline, from basic use-case handlers to advanced chain-of-responsibility and orchestration patterns.

## Handler&lt;TRequest&gt; — fire-and-forget use case

The simplest handler: receives a request, validates it, and executes logic. No return value.

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

**How it works:**
1. `HandleAsync` is called (inherited from `Handler<TRequest>`)
2. `verifier.ValidateAsync` runs all registered `AbstractValidator<TRequest>` rules in parallel
3. If `verifier.IsValid` is `false`, the handler **short-circuits** — `HandleUseCaseAsync` is never called
4. If valid, `HandleUseCaseAsync` executes your business logic

## Handler&lt;TRequest, TResponse&gt; — use case with a return value

Same pattern but returns a typed response. Returns `default` (null for reference types) when validation fails.

```csharp
// FILE: samples/DotEmilu.Samples.Domain/Contracts/GetInvoiceByIdRequest.cs
public record GetInvoiceByIdRequest(int Id) : IRequest<InvoiceResponse>;
public record InvoiceResponse(int Id, string Number, string Description, decimal Amount);
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/GetInvoiceById/GetInvoiceById.cs
internal sealed class Handler(IVerifier<GetInvoiceByIdRequest> verifier, IInvoiceQueries db)
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

## IHandler — parameterless handler

For tasks that don't need input: seed data, scheduled jobs, startup initialization.

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S08ParameterlessHandler/SeedDataHandler.cs
internal sealed class SeedDataHandler : IHandler
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("  Seeding product catalogue...");
        await Task.Delay(50, cancellationToken);  // simulate async work
        Console.WriteLine("  ✅ 12 products seeded.");
    }
}
```

> **Note:** `AddHandlers(assembly)` does not discover `IHandler` (parameterless) implementations. Register them explicitly:
> ```csharp
> // FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S08ParameterlessHandler/Container.cs
> services.AddScoped<SeedDataHandler>();
> ```

## Lifecycle hooks — HandleExceptionAsync and FinalizeAsync

Every handler inheriting from `BaseHandler` (which all `Handler<>` types do) gets two lifecycle hooks:

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S03LifecycleHooks/ProcessPaymentHandler.cs
internal sealed class ProcessPaymentHandler(IVerifier<ProcessPaymentRequest> verifier)
    : Handler<ProcessPaymentRequest>(verifier)
{
    protected override Task HandleUseCaseAsync(ProcessPaymentRequest request, CancellationToken ct)
    {
        Console.WriteLine($"  Processing payment: {request.Amount:C} {request.Currency}");

        if (request.Amount > 50_000m)
            throw new InvalidOperationException(
                $"Payment of {request.Amount:C} exceeds the single-transaction limit.");

        Console.WriteLine("  ✅ Payment processed successfully.");
        return Task.CompletedTask;
    }

    protected override Task HandleExceptionAsync(Exception e)
    {
        // Called when HandleUseCaseAsync throws
        // Runs BEFORE the exception is re-thrown
        Console.WriteLine($"  ⚠️  HandleExceptionAsync: {e.Message}");
        return Task.CompletedTask;
    }

    protected override Task FinalizeAsync()
    {
        // Called ALWAYS (like a finally block)
        // Runs after success OR after HandleExceptionAsync
        Console.WriteLine("  🧹 FinalizeAsync: releasing payment gateway session.");
        return Task.CompletedTask;
    }
}
```

**Execution flow:**

```
HandleAsync
  └─ try
  │    └─ ValidateAsync → HandleUseCaseAsync
  ├─ catch (Exception e)
  │    └─ HandleExceptionAsync(e) → re-throw
  └─ finally
       └─ FinalizeAsync()
```

## Manual validation errors

Sometimes validation depends on runtime state (database lookups, external services). Use `IVerifier` directly:

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S04ManualValidationErrors/LoginHandler.cs
internal sealed class LoginHandler(
    IVerifier<LoginRequest> verifier,
    ILoginService loginService)
    : Handler<LoginRequest, LoginResult>(verifier)
{
    private readonly IVerifier<LoginRequest> _verifier = verifier;

    protected override async Task<LoginResult?> HandleUseCaseAsync(
        LoginRequest request, CancellationToken ct)
    {
        var isAuthenticated = await loginService.AuthenticateAsync(
            request.Username, request.Password, ct);

        if (!isAuthenticated)
        {
            // Add a runtime validation error — handler returns null, HttpHandler returns 400
            _verifier.AddValidationError("Credentials", "Invalid username or password.");
            return null;
        }

        return new LoginResult(
            Token: $"tok_{request.Username}_{Guid.NewGuid():N}",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));
    }
}
```

**Available methods on `IVerifier`:**
- `AddValidationError(string propertyName, string errorMessage)` — single error
- `AddValidationError(ValidationFailure validationError)` — FluentValidation failure object
- `AddValidationErrors(List<ValidationFailure> validationErrors)` — bulk add (useful for propagation)

## ChainHandler&lt;TChain&gt; — chain of responsibility

For multi-step pipelines where each step operates on a shared context and can short-circuit.

### Step 1: Define a chain context

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S06ChainHandlerMultiStep/SyncJobContext.cs
internal sealed class SyncJobContext
{
    public required List<string> DataSourceNames { get; init; }
    public List<string> ValidationErrors { get; } = [];
    public List<string> ValidatedItems { get; } = [];
    public List<string> EnrichedPayloads { get; } = [];
    public bool IsPersisted { get; set; }
}
```

### Step 2: Implement chain handlers

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S06ChainHandlerMultiStep/ValidateJobHandler.cs
internal sealed class ValidateJobHandler : ChainHandler<SyncJobContext>
{
    public override async Task ContinueAsync(SyncJobContext chain, CancellationToken ct)
    {
        foreach (var name in chain.DataSourceNames)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Contains(' '))
                chain.ValidationErrors.Add($"Invalid source name: '{name}'");
            else
                chain.ValidatedItems.Add(name);
        }

        // Short-circuit: do not call Successor if validation fails
        if (chain.ValidationErrors.Count > 0)
            return;

        if (Successor is not null)
            await Successor.ContinueAsync(chain, ct);
    }
}
```

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S06ChainHandlerMultiStep/EnrichJobHandler.cs
internal sealed class EnrichJobHandler : ChainHandler<SyncJobContext>
{
    public override async Task ContinueAsync(SyncJobContext chain, CancellationToken ct)
    {
        foreach (var item in chain.ValidatedItems)
            chain.EnrichedPayloads.Add(
                $"{{ \"source\": \"{item}\", \"enrichedAt\": \"{DateTimeOffset.UtcNow:O}\" }}");

        if (Successor is not null)
            await Successor.ContinueAsync(chain, ct);
    }
}
```

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S06ChainHandlerMultiStep/PersistJobHandler.cs
internal sealed class PersistJobHandler : ChainHandler<SyncJobContext>
{
    public override async Task ContinueAsync(SyncJobContext chain, CancellationToken ct)
    {
        foreach (var payload in chain.EnrichedPayloads)
            Console.WriteLine($"     💾 Persisted: {payload}");

        chain.IsPersisted = true;
        await Task.CompletedTask;
    }
}
```

### Step 3: Wire the chain in a processor

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S06ChainHandlerMultiStep/SyncJobProcessor.cs
internal sealed class SyncJobProcessor(
    ValidateJobHandler validate,
    EnrichJobHandler enrich,
    PersistJobHandler persist) : IHandler
{
    private readonly ChainHandler<SyncJobContext> _first = validate;

    // Wire chain: Validate → Enrich → Persist
    private readonly ChainHandler<SyncJobContext> _ =
        validate.SetSuccessor(enrich).SetSuccessor(persist);

    public Task HandleAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException("Use RunAsync(SyncJobContext) instead.");

    public Task RunAsync(SyncJobContext context, CancellationToken ct)
        => _first.ContinueAsync(context, ct);
}
```

### Step 4: Register

```csharp
// FILE: samples/DotEmilu.Samples.ConsoleApp/Scenarios/S06ChainHandlerMultiStep/Container.cs
services
    .AddChainHandlers(Assembly.GetExecutingAssembly())  // registers ValidateJobHandler, EnrichJobHandler, PersistJobHandler
    .AddHandlers(assembly);                              // registers other IHandler<> implementations
```

**Short-circuiting:** A chain handler can stop the pipeline by simply not calling `Successor.ContinueAsync`. This is the recommended way to abort early (e.g., validation failure).

## Orchestrating handlers — composing sub-handlers

When a complex operation delegates to other handlers and needs to propagate their validation errors:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/ConfirmInvoice/ConfirmInvoice.cs
internal sealed class Handler(
    IVerifier<ConfirmInvoiceRequest> verifier,
    IHandler<ValidateInvoiceForConfirmationRequest> validateHandler,
    IVerifier<ValidateInvoiceForConfirmationRequest> validateVerifier,
    IInvoiceCommands db)
    : Handler<ConfirmInvoiceRequest, ConfirmInvoiceResponse>(verifier)
{
    private readonly IVerifier<ConfirmInvoiceRequest> _verifier = verifier;

    protected override async Task<ConfirmInvoiceResponse?> HandleUseCaseAsync(
        ConfirmInvoiceRequest request, CancellationToken ct)
    {
        // 1. Delegate to sub-handler
        await validateHandler.HandleAsync(
            new ValidateInvoiceForConfirmationRequest(request.Id), ct);

        // 2. Propagate sub-handler's validation errors
        if (!validateVerifier.IsValid)
        {
            _verifier.AddValidationErrors(validateVerifier.ValidationErrors.ToList());
            return null;
        }

        // 3. Execute confirmation logic
        var invoice = await db.Invoices.FindAsync([request.Id], ct);
        invoice!.IsPaid = true;
        await db.SaveChangesAsync(ct);

        return new ConfirmInvoiceResponse(invoice.Id, $"Invoice {invoice.Number} confirmed.");
    }
}
```

**Key pattern:** Inject both the sub-handler (`IHandler<TSubRequest>`) and its verifier (`IVerifier<TSubRequest>`). After calling the sub-handler, check the sub-verifier's `IsValid` and propagate errors with `AddValidationErrors()`.

## ASP.NET Core endpoint wiring with AsDelegate

`AsDelegate` converts a handler into a zero-boilerplate Minimal API delegate:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/InvoiceEndpoints.cs
app.MapGroup("/invoices").WithTags("Invoices")
    .MapCreateInvoice()
    .MapConfirmInvoice()
    .MapSyncInvoices()
    .MapGetInvoices()
    .MapGetInvoiceById()
    .MapUpdateInvoice()
    .MapDeleteInvoice();
```

Each feature defines its endpoint wiring inline:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/CreateInvoice/CreateInvoice.cs
// POST /invoices → 201 Created
builder.MapPost(string.Empty, AsDelegate.ForAsync<CreateInvoiceRequest>(TypedResults.Created));
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/GetInvoiceById/GetInvoiceById.cs
// GET /invoices/{id} → 200 OK or 404 Not Found (custom result function)
builder.MapGet("{id:int}", AsDelegate.ForAsync<GetInvoiceByIdRequest, InvoiceResponse>(
    response => response is null ? TypedResults.NotFound() : TypedResults.Ok(response)));
```

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/Features/DeleteInvoice/DeleteInvoice.cs
// DELETE /invoices/{id} → 204 No Content
builder.MapDelete("{id:int}", AsDelegate.ForAsync<DeleteInvoiceRequest>(TypedResults.NoContent));
```

## DI registration summary

```csharp
services
    // Core verifier (IVerifier<> → Verifier<> that aggregates FluentValidation validators)
    .AddVerifier()

    // Scan assembly for IHandler<> and IHandler<,> implementations
    .AddHandlers(Assembly.GetExecutingAssembly())

    // Scan assembly for ChainHandler<> subclasses
    .AddChainHandlers(Assembly.GetExecutingAssembly())

    // FluentValidation validators
    .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
```

For ASP.NET Core, `AddDotEmilu()` replaces `AddVerifier()` and additionally registers `HttpHandler<>`, `HttpHandler<,>`, and `IPresenter`:

```csharp
// FILE: samples/DotEmilu.Samples.FullApp/DiContainers/WebApiContainer.cs
services
    .AddDotEmilu()   // HttpHandler + Presenter + Verifier
    .AddOpenApi();
```

## Pattern decision tree

```
Need to handle a request?
├── No input needed → IHandler (parameterless)
├── No return value → Handler<TRequest>
├── With return value → Handler<TRequest, TResponse>
├── Multi-step pipeline with shared context → ChainHandler<TChain>
└── Composing sub-handlers → Orchestrating Handler (inject IHandler + IVerifier of sub-handler)
```
