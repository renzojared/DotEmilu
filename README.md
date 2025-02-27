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
   public class InHandlerUseCase(IVerifier<InHandlerDto> verifier) : Handler<InHandlerDto>(verifier)
   {
   private readonly IVerifier<InHandlerDto> _verifier = verifier;
   
       protected override async Task HandleUseCaseAsync(InHandlerDto request, CancellationToken cancellationToken)
       {
           //Adding validations
           if (request.Day == 1)
           {
               _verifier.AddError("Day", "Not valid day");
               Console.WriteLine("Not valid day");
               return;
           }
   
           if (request.Day == 2)
               throw new AggregateException("Not valid day Handler");
   
           await Task.Delay(1000, cancellationToken);
       }
   
       protected override async Task HandleExceptionAsync(Exception e)
       {
           //Works with exception async
           if (e is AggregateException aggregateException)
           {
               Console.WriteLine("AggregateException", aggregateException);
               await Task.Delay(100);
           }
       }
   }
    ```

2. **Add Your Endpoint**  
   Define your endpoint using either Minimal API or a Controller, depending on your project's structure.

    ```csharp
    private static async Task<IResult> InHandlerCase([FromBody] InHandlerDto dto,
        HttpHandler<InHandlerDto> handler,
        CancellationToken cancellationToken)
        => await handler.HandleAsync(dto, cancellationToken, Results.NoContent);
    ```

3. **Register Your Dependencies**  
   Ensure all required dependencies are registered in your application's dependency injection container.

    ```csharp
    builder.Services
        .AddDotEmilu()
        .AddHandlers(Assembly.GetExecutingAssembly())
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
