namespace DotEmilu.UseCaseTest.Endpoints;

public static class SampleEndpoint
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app
            .MapGroup("api/sample")
            .WithTags("sample")
            .WithOpenApi()
            .MapSampleGroup();
        return app;
    }

    private static RouteGroupBuilder MapSampleGroup(this RouteGroupBuilder builder)
    {
        builder
            .MapPost("test", Sample)
            .Produces<SampleResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return builder;
    }

    private static async Task<IResult> Sample([FromBody] SampleRequest request,
        HttpHandler<SampleRequest, SampleResponse> handler,
        CancellationToken cancellationToken)
        => await handler.HandleAsync(request, cancellationToken, result =>
        {
            if (request.Date.Year == 2025)
                return Results.Ok($"Congratulations! {result}");

            if (request.Category >= 10)
                return Results.Ok($"{result}. Account: {request.Account}. Category: {request.Category}");

            return Results.Ok(result);
        });
}