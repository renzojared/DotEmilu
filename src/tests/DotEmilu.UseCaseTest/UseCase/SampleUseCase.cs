namespace DotEmilu.UseCaseTest.UseCase;

public class SampleUseCase(IVerifier<SampleRequest> verifier, IPresenter presenter)
    : Handler<SampleRequest, SampleResponse>(verifier, presenter)
{
    private readonly IPresenter _presenter = presenter;
    private readonly IVerifier<SampleRequest> _verifier = verifier;

    protected override async Task<SampleResponse?> HandleResponseAsync(SampleRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await SomeMethod(request, cancellationToken);

        if (string.IsNullOrEmpty(request.Note))
        {
            _verifier.AddError("request", "invalid request");
            return null;
        }

        if (request.Date.Year == 2024)
            return ResultIn(Results.Ok($"Congratulations! {result}"));

        if (request.Category >= 10)
            return ResultIn(_presenter.Success($"{result}. Account: {request.Account}. Category: {request.Category}"));

        return new SampleResponse(result);
    }

    private async Task<string> SomeMethod(SampleRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(3000, cancellationToken);
        var message = $"El día {request.Date.ToShortDateString()} se hizo un cargo de {request.Amount}";
        Console.WriteLine(message);
        return message;
    }
}