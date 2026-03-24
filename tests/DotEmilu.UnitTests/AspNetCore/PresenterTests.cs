using System.Text;
using System.Text.Json;
using DotEmilu.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DotEmilu.UnitTests.AspNetCore;

public sealed class PresenterTests
{
    [Fact]
    public async Task ValidationError_WhenGroupedByProperty_ThenReturnsBadRequestAndDefaultType()
    {
        var provider = BuildProvider(environmentName: Environments.Production);
        var presenter = provider.GetRequiredService<IPresenter>();

        var failures = new[]
        {
            new ValidationFailure("Name", "required"),
            new ValidationFailure("Name", "too long"),
            new ValidationFailure("Age", "invalid")
        };

        var result = presenter.ValidationError(failures);
        var executed = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status400BadRequest, executed.StatusCode);

        using var json = JsonDocument.Parse(executed.Body);
        var errors = json.RootElement.GetProperty("errors");

        Assert.True(errors.TryGetProperty("Name", out var nameErrors));
        Assert.Equal(2, nameErrors.GetArrayLength());

        Assert.True(errors.TryGetProperty("Age", out var ageErrors));
        Assert.Equal(1, ageErrors.GetArrayLength());
        Assert.Equal("https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            json.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task ServerError_WhenProduction_ThenDoesNotExposeExceptionDetailsAndUsesDefaultType()
    {
        var provider = BuildProvider(environmentName: Environments.Production);
        var presenter = provider.GetRequiredService<IPresenter>();

        var result = presenter.ServerError(new InvalidOperationException("boom"));
        var executed = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status500InternalServerError, executed.StatusCode);

        using var json = JsonDocument.Parse(executed.Body);
        var detail = json.RootElement.GetProperty("detail").GetString() ?? string.Empty;
        Assert.DoesNotContain("boom", detail);
        Assert.Equal("https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            json.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task ServerError_WhenDevelopment_ThenIncludesExceptionDetails()
    {
        var provider = BuildProvider(environmentName: Environments.Development);
        var presenter = provider.GetRequiredService<IPresenter>();

        var result = presenter.ServerError(new InvalidOperationException("boom"));
        var executed = await ExecuteAsync(result);

        Assert.Equal(StatusCodes.Status500InternalServerError, executed.StatusCode);

        using var json = JsonDocument.Parse(executed.Body);
        var detail = json.RootElement.GetProperty("detail").GetString() ?? string.Empty;
        Assert.Contains("boom", detail);
    }

    [Fact]
    public async Task ValidationError_WhenCustomTypeConfigured_ThenUsesConfiguredType()
    {
        const string customType = "https://errors.test/validation";
        var provider = BuildProvider(Environments.Production, validationType: customType);
        var presenter = provider.GetRequiredService<IPresenter>();

        var result = presenter.ValidationError([new ValidationFailure("Name", "required")]);
        var executed = await ExecuteAsync(result);

        using var json = JsonDocument.Parse(executed.Body);
        Assert.Equal(customType, json.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public async Task ServerError_WhenCustomTypeConfigured_ThenUsesConfiguredType()
    {
        const string customType = "https://errors.test/server";
        var provider = BuildProvider(Environments.Production, serverType: customType);
        var presenter = provider.GetRequiredService<IPresenter>();

        var result = presenter.ServerError(new InvalidOperationException("boom"));
        var executed = await ExecuteAsync(result);

        using var json = JsonDocument.Parse(executed.Body);
        Assert.Equal(customType, json.RootElement.GetProperty("type").GetString());
    }

    [Fact]
    public void AddDotEmilu_WhenResultMessageConfigurationIsMissing_ThenThrowsOnOptionsAccess()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Unrelated:Key"] = "Value"
        });
        builder.Services.AddDotEmilu();

        using var host = builder.Build();

        Assert.Throws<OptionsValidationException>(() =>
            _ = host.Services.GetRequiredService<IOptions<ResultMessage>>().Value);
    }

    private static ServiceProvider BuildProvider(string environmentName, string? validationType = null,
        string? serverType = null)
    {
        var services = new ServiceCollection();

        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environmentName);
        services.AddSingleton(env);

        var values = new Dictionary<string, string?>
        {
            ["ResultMessage:ValidationError:Title"] = "Bad Request",
            ["ResultMessage:ValidationError:Detail"] = "Validation failed",
            ["ResultMessage:ServerError:Title"] = "Server Error",
            ["ResultMessage:ServerError:Detail"] = "Unexpected error"
        };

        if (!string.IsNullOrWhiteSpace(validationType))
            values["ResultMessage:ValidationError:Type"] = validationType;

        if (!string.IsNullOrWhiteSpace(serverType))
            values["ResultMessage:ServerError:Type"] = serverType;

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build());

        services.AddDotEmilu();
        return services.BuildServiceProvider();
    }

    private static async Task<(int StatusCode, string Body)> ExecuteAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServices();
        await using var bodyStream = new MemoryStream();
        httpContext.Response.Body = bodyStream;

        await result.ExecuteAsync(httpContext);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        return (httpContext.Response.StatusCode, body);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<JsonOptions>(_ => { });
        return services.BuildServiceProvider();
    }
}
