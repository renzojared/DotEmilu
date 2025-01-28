# DotEmilu

A simple, easy-to-use .NET library designed for handling HTTP requests and responses.

## Features

- Supports **GET**, **POST**, **PUT**, and **DELETE** requests.
- Handles HTTP responses with status codes and error handling.
- Built with **.NET 8** for cross-platform compatibility (Windows, Linux, macOS).
- Supports **async/await** for non-blocking HTTP calls.

## How to Use

Follow these simple steps to get started:

1. **Build Your Logic**  
   Create the logic for your application based on your previously defined request and expected response.

    ```csharp
    public class SampleUseCase(IVerifier<SampleRequest> verifier, IPresenter presenter)
        : Handler<SampleRequest, SampleResponse>(verifier, presenter)
    {
        private readonly IPresenter _presenter = presenter;
        private readonly IVerifier<SampleRequest> _verifier = verifier;
    
        protected override async Task<SampleResponse?> HandleResponseAsync(SampleRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await SomeMethod(request, cancellationToken);
    
            // Add custom validations
            if (string.IsNullOrEmpty(request.Note))
            {
                _verifier.AddError("request", "invalid request");
                return null;
            }
    
            // To custom response you can use Results
            if (request.Date.Year == 2024)
                return ResultIn(Results.Ok($"Congratulations! {result}"));
    
            // Or standard response defined in IPresenter
            if (request.Category >= 10)
                return ResultIn(_presenter.Success($"{result}. Account: {request.Account}. Category: {request.Category}"));
    
            // By default you need return the TResponse
            return new SampleResponse(result);
        }
    
        private async Task<string> SomeMethod(SampleRequest request, CancellationToken cancellationToken = default)
        {
            // Some logic
        }
    }
    ```

2. **Add Your Endpoint**  
   Define your endpoint using either Minimal API or a Controller, depending on your project's structure.

    ```csharp
    app.MapPost("/api/sample", async ([FromBody] SampleRequest request,
        IHandler<SampleRequest> handler,
        CancellationToken cancellationToken) => await handler.HandleAsync(request, cancellationToken));
    ```

3. **Register Your Dependencies**  
   Ensure all required dependencies are registered in your application's dependency injection container.

    ```csharp
    builder.Services
        .AddDotEmilu()
        .AddScoped<IHandler<SampleRequest>, SampleUseCase>()
        .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    ```

For a [complete example](https://github.com/renzojared/DotEmilu/tree/main/src/tests/DotEmilu.UseCaseTest)

## Notes:

You need to add the response messages for bad request (400) and server error (500)

```json
{
  "ResultMessage": {
    "ValidationError": {
      "Title": "Bad Request",
      "Detail": "One or more errors"
    },
    "ServerError": {
      "Title": "Server Error",
      "Detail": "Please contact to administrator"
    }
  }
}
```

And for complex validation use fluent validation

```csharp
public class SampleValidator : AbstractValidator<SampleRequest>
{
    public SampleValidator()
    {
        RuleFor(s => s.Date)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Now));

        RuleFor(s => s.Amount)
            .GreaterThan(0);
    }
}
```