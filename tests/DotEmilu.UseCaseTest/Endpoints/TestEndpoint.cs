namespace DotEmilu.UseCaseTest.Endpoints;

public static class TestEndpoint
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app
            .MapGroup("api")
            .WithOpenApi()
            .MapGroups();
        return app;
    }
}

public static class MapEndpoint
{
    public static RouteGroupBuilder MapGroups(this RouteGroupBuilder builder)
    {
        builder.MapPost("in-case", InCase);
        builder.MapPost("in-out-case", InOutCase);
        builder.MapPost("in-handler-case", InHandlerCase);
        builder.MapPost("in-out-handler-case", InOutHandlerCase);
        builder.MapPost("full-case", FullCase);

        return builder;
    }

    private static async Task<IResult> InCase([FromBody] InDto dto,
        IHandler<InDto> handler,
        IVerifier<InDto> verifier,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(dto, cancellationToken);
        return Results.Ok(verifier.Errors);
    }

    private static async Task<IResult> InOutCase([FromBody] InOutDto dto,
        IHandler<InOutDto, InOutDtoResponse> handler,
        IVerifier<InOutDto> verifier,
        CancellationToken cancellationToken)
    {
        var response = await handler.HandleAsync(dto, cancellationToken);
        if (!verifier.IsValid)
            return Results.BadRequest(verifier.Errors);

        return Results.Ok(response);
    }

    private static async Task<IResult> InHandlerCase([FromBody] InHandlerDto dto,
        HttpHandler<InHandlerDto> handler,
        CancellationToken cancellationToken)
        => await handler.HandleAsync(dto, cancellationToken, Results.NoContent);

    private static async Task<IResult> InOutHandlerCase([FromBody] InOutHandlerDto dto,
        HttpHandler<InOutHandlerDto, InOutHandlerDtoResponse> handler,
        CancellationToken cancellationToken)
        => await handler.HandleAsync(dto, cancellationToken);

    private static async Task<IResult> FullCase([FromBody] FullDto dto,
        HttpHandler<FullDto, FullOutDto> handler,
        CancellationToken cancellationToken)
        => await handler.HandleAsync(dto, cancellationToken, _ => Results.NoContent());
}