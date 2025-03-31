# DotEmilu

A simple, easy-to-use .NET library designed for handling HTTP requests and responses.

## All packages available

- [DotEmilu.Abstractions](https://www.nuget.org/packages/DotEmilu.Abstractions)
- [DotEmilu](https://www.nuget.org/packages/DotEmilu)
- [DotEmilu.AspNetCore](https://www.nuget.org/packages/DotEmilu.AspNetCore)

## Features

- Supports **GET**, **POST**, **PUT**, and **DELETE** requests.
- Handles HTTP responses with status codes and error handling.
- Built with **.NET 8** for cross-platform compatibility (Windows, Linux, macOS).
- Supports **async/await** for non-blocking HTTP calls.
- Includes request contracts and base constraints for verifiers.
- Provides an **HttpHandler** to simplify request handling.
- New extension method to register handlers using reflection for cleaner setup.

## How to Use

Follow these simple steps to get started:

1. **Build Your Logic**  
   Create the logic for your application based on your previously defined request and expected response.

   ```csharp
   public class FullUseCase(
       IVerifier<FullDto> verifier,
       IVerifier<InDto> verifierIn,
       IHandler<InDto> handlerIn)
       : Handler<FullDto, FullOutDto>(verifier)
   {
       private readonly IVerifier<FullDto> _verifier = verifier;
   
       protected override async Task<FullOutDto?> HandleUseCaseAsync(FullDto request, CancellationToken cancellationToken)
       {
           Console.WriteLine("Handling my primary use case...");
   
           var requestIn = new InDto(request.Day);
   
           await WorksSecondCaseAsync(requestIn, cancellationToken);
   
           if (!verifierIn.IsValid)
           {
               _verifier.AddErrors(verifierIn.Errors.ToList());
               _verifier.AddError("BehindCase", "Second case has errors");
               return null;
           }
   
           return new FullOutDto();
       }
   
       private async Task WorksSecondCaseAsync(InDto request, CancellationToken cancellationToken)
           => await handlerIn.HandleAsync(request, cancellationToken);
   }
   ```

2. **Add Your Endpoint**  
   Define your endpoint using either Minimal API or a Controller, depending on your project's structure.

   ```csharp
       private static async Task<IResult> FullCase([FromBody] FullDto dto,
           HttpHandler<FullDto, FullOutDto> handler,
           CancellationToken cancellationToken)
           => await handler.HandleAsync(dto, cancellationToken, _ => Results.NoContent());
   ```

3. **Register Your Dependencies**  
   Ensure all required dependencies are registered in your application's dependency injection container.

   ```csharp
   builder.Services
       .AddDotEmilu()
       .AddHandlers(Assembly.GetExecutingAssembly())
       .AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
   ```

For a [complete example](https://github.com/renzojared/DotEmilu/tree/main/tests/DotEmilu.UseCaseTest)

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
