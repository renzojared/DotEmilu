namespace DotEmilu.UseCaseTest.UseCases;

public class InOutHandlerUseCase(IVerifier<InOutHandlerDto> verifier)
    : Handler<InOutHandlerDto, InOutHandlerDtoResponse>(verifier)
{
    protected override Task<InOutHandlerDtoResponse?> HandleUseCaseAsync(InOutHandlerDto request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("InOutHandlerUseCase");

        return Task.FromResult<InOutHandlerDtoResponse?>(new InOutHandlerDtoResponse(1));
    }
}