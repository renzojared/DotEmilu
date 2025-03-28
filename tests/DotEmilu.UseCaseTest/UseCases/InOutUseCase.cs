namespace DotEmilu.UseCaseTest.UseCases;

public class InOutUseCase(IVerifier<InOutDto> verifier) : Handler<InOutDto, InOutDtoResponse>(verifier)
{
    private readonly IVerifier<InOutDto> _verifier = verifier;

    protected override async Task<InOutDtoResponse?> HandleUseCaseAsync(InOutDto request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);

        if (request.Day == 2)
        {
            Console.WriteLine("Has error");
            _verifier.AddError("Day", "Sample error");
            return null;
        }

        return new InOutDtoResponse();
    }
}