namespace DotEmilu.UseCaseTest.UseCase;

public class SampleUseCase(IVerifier<SampleRequest> verifier)
    : Handler<SampleRequest, SampleResponse>(verifier)
{
    private readonly IVerifier<SampleRequest> _verifier = verifier;

    protected override async Task<SampleResponse?> HandleUseCaseAsync(SampleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await SomeMethod(request, cancellationToken);

        if (string.IsNullOrEmpty(request.Note))
        {
            _verifier.AddError("request", "invalid request");
            return null;
        }

        return new SampleResponse(result);
    }

    private async Task<string> SomeMethod(SampleRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(3000, cancellationToken);
        var message = $"El día {request.Date.ToShortDateString()} se hizo un cargo de {request.Amount}";
        Console.WriteLine(message);
        return message;
    }
}