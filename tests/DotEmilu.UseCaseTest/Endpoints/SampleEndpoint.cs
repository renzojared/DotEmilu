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
        IHandler<SampleRequest> handler,
        CancellationToken cancellationToken)
        => await handler.HandleAsync(request, cancellationToken);
}