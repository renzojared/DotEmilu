namespace DotEmilu.UseCaseTest.UseCases;

public class InUseCase(IVerifier<InDto> verifier) : Handler<InDto>(verifier)
{
    protected override async Task HandleUseCaseAsync(InDto dto, CancellationToken cancellationToken)
    {
        Console.WriteLine("InUseCase");
        await Task.Delay(1000, cancellationToken);
    }

    protected override Task HandleExceptionAsync(Exception e)
    {
        e = new Exception("InUseCaseException");
        Console.WriteLine("InUseCaseException");
        return Task.CompletedTask;
    }

    protected override Task FinalizeAsync()
    {
        Console.WriteLine("InUseCaseFinalized");
        return Task.CompletedTask;
    }
}