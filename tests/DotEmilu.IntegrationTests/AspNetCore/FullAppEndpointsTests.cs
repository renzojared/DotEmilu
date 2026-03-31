using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DotEmilu.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DotEmilu.IntegrationTests.AspNetCore;

public sealed class FullAppEndpointsTests
{
    [Fact]
    public async Task GetInvoiceById_WhenEntityExists_ThenReturns200WithPayload()
    {
        await using var app = await BuildAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/invoices/1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var json =
            await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, json.RootElement.GetProperty("id").GetInt32());
        Assert.Equal("INV-001", json.RootElement.GetProperty("number").GetString());
    }

    [Fact]
    public async Task GetInvoiceById_WhenEntityDoesNotExist_ThenReturns404()
    {
        await using var app = await BuildAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/invoices/999999", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ConfirmInvoice_WhenSubHandlerSucceeds_ThenReturns200WithConfirmationCode()
    {
        await using var app = await BuildAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/invoices/1/confirm",
            new ConfirmInvoiceBody("approved"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var json =
            await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, json.RootElement.GetProperty("invoiceId").GetInt32());
        Assert.Equal("CONF-001", json.RootElement.GetProperty("confirmationCode").GetString());
    }

    [Fact]
    public async Task ConfirmInvoice_WhenSubHandlerAddsErrors_ThenReturns400ProblemDetails()
    {
        await using var app = await BuildAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/invoices/999999/confirm",
            new ConfirmInvoiceBody("attempt confirmation"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var json =
            await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Bad Request", json.RootElement.GetProperty("title").GetString());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("InvoiceId", out _));
    }

    [Fact]
    public async Task SyncInvoices_WhenValidRequest_ThenReturns200WithSyncedCount()
    {
        await using var app = await BuildAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/invoices/sync",
            new SyncInvoicesRequest([10, 20, 30]),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var json =
            await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, json.RootElement.GetProperty("syncedCount").GetInt32());
        Assert.True(json.RootElement.GetProperty("isPersisted").GetBoolean());
    }

    [Fact]
    public async Task SyncInvoices_WhenTargetIdsAreEmpty_ThenReturns400ProblemDetails()
    {
        await using var app = await BuildAppAsync();
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/invoices/sync",
            new SyncInvoicesRequest(Array.Empty<int>()),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        using var json =
            await JsonDocument.ParseAsync(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Bad Request", json.RootElement.GetProperty("title").GetString());
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("SyncJob", out _));
    }

    private static async Task<WebApplication> BuildAppAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            { EnvironmentName = Environments.Production });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ResultMessage:ValidationError:Title"] = "Bad Request",
            ["ResultMessage:ValidationError:Detail"] = "Validation failed",
            ["ResultMessage:ServerError:Title"] = "Server Error",
            ["ResultMessage:ServerError:Detail"] = "Unexpected error"
        });

        builder.Services
            .AddDotEmilu()
            .AddHandlers(typeof(FullAppEndpointsTests).Assembly)
            .AddChainHandlers(typeof(FullAppEndpointsTests).Assembly);

        var app = builder.Build();

        app.MapGet("/invoices/{id:int}",
            (int id, HttpHandler<GetInvoiceByIdRequest, InvoiceResponse> handler, CancellationToken ct) =>
                AsDelegate.ForAsync<GetInvoiceByIdRequest, InvoiceResponse>(result =>
                    result is null ? TypedResults.NotFound() : TypedResults.Ok(result))(
                    new GetInvoiceByIdRequest(id), handler, ct));

        app.MapPost("/invoices/{id:int}/confirm",
            (int id, ConfirmInvoiceBody body, HttpHandler<ConfirmInvoiceRequest, ConfirmInvoiceResponse> handler,
                    CancellationToken ct) =>
                AsDelegate.ForAsync<ConfirmInvoiceRequest, ConfirmInvoiceResponse>(TypedResults.Ok)(
                    new ConfirmInvoiceRequest(id, body.ConfirmationNotes), handler, ct));

        app.MapPost("/invoices/sync",
            (SyncInvoicesRequest request, HttpHandler<SyncInvoicesRequest, SyncInvoicesResponse> handler,
                    CancellationToken ct) =>
                AsDelegate.ForAsync<SyncInvoicesRequest, SyncInvoicesResponse>(TypedResults.Ok)(request, handler, ct));

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private sealed record GetInvoiceByIdRequest(int Id) : IRequest<InvoiceResponse>;

    private sealed record InvoiceResponse(int Id, string Number);

    private sealed class GetInvoiceByIdHandler(IVerifier<GetInvoiceByIdRequest> verifier)
        : Handler<GetInvoiceByIdRequest, InvoiceResponse>(verifier)
    {
        protected override Task<InvoiceResponse?> HandleUseCaseAsync(GetInvoiceByIdRequest request,
            CancellationToken cancellationToken)
            => Task.FromResult<InvoiceResponse?>(request.Id == 1 ? new InvoiceResponse(1, "INV-001") : null);
    }

    private sealed record ConfirmInvoiceRequest(int InvoiceId, string ConfirmationNotes)
        : IRequest<ConfirmInvoiceResponse>;

    private sealed record ConfirmInvoiceResponse(int InvoiceId, string ConfirmationCode);

    private sealed record ConfirmInvoiceBody(string ConfirmationNotes);

    private sealed record ValidateInvoiceForConfirmationRequest(int InvoiceId) : IRequest;

    private sealed class ConfirmInvoiceHandler(
        IVerifier<ConfirmInvoiceRequest> verifier,
        IHandler<ValidateInvoiceForConfirmationRequest> subHandler,
        IVerifier<ValidateInvoiceForConfirmationRequest> subVerifier)
        : Handler<ConfirmInvoiceRequest, ConfirmInvoiceResponse>(verifier)
    {
        private readonly IVerifier<ConfirmInvoiceRequest> _verifier = verifier;

        protected override async Task<ConfirmInvoiceResponse?> HandleUseCaseAsync(
            ConfirmInvoiceRequest request,
            CancellationToken cancellationToken)
        {
            await subHandler.HandleAsync(new ValidateInvoiceForConfirmationRequest(request.InvoiceId),
                cancellationToken);

            if (!subVerifier.IsValid)
            {
                _verifier.AddValidationErrors(subVerifier.ValidationErrors.ToList());
                return null;
            }

            return new ConfirmInvoiceResponse(request.InvoiceId, "CONF-001");
        }
    }

    private sealed class ValidateInvoiceForConfirmationHandler(
        IVerifier<ValidateInvoiceForConfirmationRequest> verifier)
        : Handler<ValidateInvoiceForConfirmationRequest>(verifier)
    {
        private readonly IVerifier<ValidateInvoiceForConfirmationRequest> _verifier = verifier;

        protected override Task HandleUseCaseAsync(ValidateInvoiceForConfirmationRequest request,
            CancellationToken cancellationToken)
        {
            if (request.InvoiceId != 1)
                _verifier.AddValidationError("InvoiceId", $"Invoice with Id {request.InvoiceId} was not found.");

            return Task.CompletedTask;
        }
    }

    private sealed record SyncInvoicesRequest(int[] InvoiceIds) : IRequest<SyncInvoicesResponse>;

    private sealed record SyncInvoicesResponse(string JobId, int SyncedCount, bool IsPersisted);

    private sealed class SyncInvoicesContext : IRequest
    {
        internal string SyncJobId { get; init; } = "SYNC-001";
        internal required int[] TargetInvoiceIds { get; init; }
        internal List<string> ValidationErrors { get; } = [];
        internal int SyncedCount { get; set; }
        internal bool IsPersisted { get; set; }
    }

    private sealed class SyncInvoicesHandler(
        IVerifier<SyncInvoicesRequest> verifier,
        IHandler<SyncInvoicesContext> processor)
        : Handler<SyncInvoicesRequest, SyncInvoicesResponse>(verifier)
    {
        private readonly IVerifier<SyncInvoicesRequest> _verifier = verifier;

        protected override async Task<SyncInvoicesResponse?> HandleUseCaseAsync(
            SyncInvoicesRequest request,
            CancellationToken cancellationToken)
        {
            var context = new SyncInvoicesContext { TargetInvoiceIds = request.InvoiceIds };
            await processor.HandleAsync(context, cancellationToken);

            if (context.ValidationErrors.Count == 0)
                return new SyncInvoicesResponse(context.SyncJobId, context.SyncedCount, context.IsPersisted);

            foreach (var error in context.ValidationErrors)
                _verifier.AddValidationError("SyncJob", error);

            return null;
        }
    }

    private sealed class SyncInvoicesProcessor(
        ValidateSyncPreconditionsHandler validateHandler,
        ApplySyncHandler applyHandler)
        : IHandler<SyncInvoicesContext>
    {
        public async Task HandleAsync(SyncInvoicesContext request, CancellationToken cancellationToken)
        {
            validateHandler.SetSuccessor(applyHandler);
            await validateHandler.ContinueAsync(request, cancellationToken);
        }
    }

    private sealed class ValidateSyncPreconditionsHandler : ChainHandler<SyncInvoicesContext>
    {
        public override async Task ContinueAsync(SyncInvoicesContext chain, CancellationToken cancellationToken)
        {
            if (chain.TargetInvoiceIds.Length == 0)
            {
                chain.ValidationErrors.Add("No target invoices specified for synchronization.");
                return;
            }

            if (Successor is not null)
                await Successor.ContinueAsync(chain, cancellationToken);
        }
    }

    private sealed class ApplySyncHandler : ChainHandler<SyncInvoicesContext>
    {
        public override Task ContinueAsync(SyncInvoicesContext chain, CancellationToken cancellationToken)
        {
            chain.SyncedCount = chain.TargetInvoiceIds.Length;
            chain.IsPersisted = true;
            return Task.CompletedTask;
        }
    }
}
