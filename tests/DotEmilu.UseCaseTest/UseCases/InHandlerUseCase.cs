namespace DotEmilu.UseCaseTest.UseCases;

public class InHandlerUseCase(IVerifier<InHandlerDto> verifier) : Handler<InHandlerDto>(verifier)
{
    private readonly IVerifier<InHandlerDto> _verifier = verifier;

    protected override async Task HandleUseCaseAsync(InHandlerDto request, CancellationToken cancellationToken)
    {
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
        if (e is AggregateException aggregateException)
        {
            Console.WriteLine("AggregateException", aggregateException);
            await Task.Delay(100);
        }
    }
}