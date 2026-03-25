# DotEmilu.Samples.ConsoleApp

Console application demonstrating all core handler patterns without ASP.NET Core. Each scenario is self-contained with its own DI setup, request contracts, validators, and handlers.

## Run

```bash
cd samples/DotEmilu.Samples.ConsoleApp
dotnet run
```

All 8 scenarios execute sequentially and print results to the console.

## Scenarios

### S01 — Basic Handler

**Pattern:** `Handler<TRequest>` (no response)

Demonstrates the fundamental handler pattern: define a request, add FluentValidation rules, implement `HandleUseCaseAsync`, and register via DI.

- Shows invalid request path (validation errors collected, handler never executes)
- Shows valid request path (validation passes, use case executes)

### S02 — Handler with Response

**Pattern:** `Handler<TRequest, TResponse>`

Same as S01 but returns a typed response. When validation fails, the handler returns `default` (null).

- Shows invalid request → null response
- Shows valid request → typed `string` response

### S03 — Lifecycle Hooks

**Pattern:** `HandleExceptionAsync` + `FinalizeAsync`

Demonstrates the `BaseHandler` lifecycle hooks. The handler processes a payment request and shows:

- Success path: `HandleUseCaseAsync` → `FinalizeAsync`
- Exception path: `HandleUseCaseAsync` → `HandleExceptionAsync` → `FinalizeAsync` → re-throw

### S04 — Manual Validation Errors

**Pattern:** `IVerifier.AddValidationError()`

Combines structural FluentValidation rules with runtime/semantic validation. The login handler:

1. Validates request structure (non-empty email/password) via `AbstractValidator`
2. Checks credentials at runtime and adds errors via `_verifier.AddValidationError("Credentials", "...")`

Shows three paths: structural failure, semantic failure, success.

### S05 — Chain Handler (Simple)

**Pattern:** `ChainHandler<TChain>` with `SetSuccessor()`

Introduces chain-of-responsibility with a simple logging chain handler:

- Single-step chain execution
- Two-step chain manually wired via `SetSuccessor()`

### S06 — Chain Handler (Multi-Step Pipeline)

**Pattern:** Validate → Enrich → Persist pipeline

Full chain-of-responsibility with a `SyncJobContext` that flows through three handlers:

- `ValidateJobHandler` — validates data sources, can short-circuit
- `EnrichJobHandler` — enriches validated items with timestamps
- `PersistJobHandler` — persists enriched payloads

Demonstrates: full pipeline, short-circuit (validation failure stops chain), and partial pipeline execution.

### S07 — Orchestrating Handler

**Pattern:** Handler composing sub-handlers with error propagation

`ProcessOrderHandler` orchestrates `ValidateOrderHandler`:

1. Calls the sub-handler via `IHandler<ValidateOrderRequest>`
2. Checks the sub-handler's verifier (`IVerifier<ValidateOrderRequest>`)
3. Propagates errors via `_verifier.AddValidationErrors(validateVerifier.ValidationErrors.ToList())`

Shows: success, sub-handler validation failure, outer handler validation failure.

### S08 — Parameterless Handler

**Pattern:** `IHandler` (no request)

`SeedDataHandler` implements `IHandler` (parameterless) for tasks like seeding data. Demonstrates:

- Explicit DI registration (assembly scanning doesn't discover `IHandler`)
- Stateless re-usability (call multiple times, same behavior)

## Project structure

```
DotEmilu.Samples.ConsoleApp/
├── Program.cs                        # Runs S01-S08 sequentially
├── Scenarios/
│   ├── IScenario.cs                  # Interface for scenario implementations
│   ├── Print.cs                      # Console formatting helpers
│   ├── S01BasicHandler/
│   │   ├── Container.cs              # DI setup
│   │   ├── CreateInvoiceHandler.cs
│   │   ├── CreateInvoiceValidator.cs
│   │   └── Scenario.cs
│   ├── S02HandlerWithResponse/
│   ├── S03LifecycleHooks/
│   ├── S04ManualValidationErrors/
│   ├── S05ChainHandlerSimple/
│   ├── S06ChainHandlerMultiStep/
│   ├── S07OrchestratingHandler/
│   └── S08ParameterlessHandler/
```
