namespace DotEmilu.UseCaseTest.UseCases;

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
            _verifier.AddValidationErrors(verifierIn.ValidationErrors.ToList());
            _verifier.AddValidationError("BehindCase", "Second case has errors");
            return null;
        }

        return new FullOutDto();
    }

    private async Task WorksSecondCaseAsync(InDto request, CancellationToken cancellationToken)
        => await handlerIn.HandleAsync(request, cancellationToken);
}