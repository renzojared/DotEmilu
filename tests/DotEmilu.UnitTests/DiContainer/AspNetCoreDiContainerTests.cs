using DotEmilu.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotEmilu.UnitTests.DiContainer;

public class AspNetCoreDiContainerTests
{
    [Fact]
    public void AddDotEmilu_WhenCalled_ThenRegistersPresenterAndHttpHandlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IHostEnvironment>());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ResultMessage:ValidationError:Title"] = "Bad Request",
                ["ResultMessage:ValidationError:Detail"] = "Validation failed",
                ["ResultMessage:ServerError:Title"] = "Server Error",
                ["ResultMessage:ServerError:Detail"] = "Unexpected error"
            })
            .Build());

        services.AddDotEmilu();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IPresenter>());
        Assert.Contains(services, s => s.ServiceType == typeof(HttpHandler<>));
        Assert.Contains(services, s => s.ServiceType == typeof(HttpHandler<,>));
    }

    [Fact]
    public void AddDotEmilu_WhenCalled_ThenRegistersOpenGenericIVerifier()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IHostEnvironment>());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ResultMessage:ValidationError:Title"] = "T",
                ["ResultMessage:ValidationError:Detail"] = "D",
                ["ResultMessage:ServerError:Title"] = "T",
                ["ResultMessage:ServerError:Detail"] = "D"
            })
            .Build());

        services.AddDotEmilu();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IVerifier<TestRequest>>());
    }

    private sealed record TestRequest : IRequest;
}
