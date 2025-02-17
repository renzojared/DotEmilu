namespace DotEmilu.UseCaseTest.UseCases;

public class InOutHandlerUseCase(IVerifier<InOutHandlerDto> verifier)
    : Handler<InOutHandlerDto, InOutHandlerDtoResponse>(verifier)
{
    protected override Task<InOutHandlerDtoResponse?> HandleUseCaseAsync(InOutHandlerDto request,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("InOutHandlerUseCase");
        // No hay necesidad de 'await', solo retornamos el valor
        return Task.FromResult<InOutHandlerDtoResponse?>(new InOutHandlerDtoResponse(1));
    }
}