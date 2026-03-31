using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu.UnitTests.DiContainer;

public class CoreDiContainerTests
{
    [Fact]
    public void AddVerifier_WhenCalled_ThenRegistersOpenGenericIVerifier()
    {
        var services = new ServiceCollection();

        services.AddVerifier();

        var provider = services.BuildServiceProvider();
        var verifier = provider.GetService<IVerifier<TestRequest>>();
        Assert.NotNull(verifier);
    }

    [Fact]
    public void AddHandlers_WhenCalled_ThenRegistersAllHandlersFromAssembly()
    {
        var services = new ServiceCollection();
        services.AddVerifier();

        services.AddHandlers(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IHandler<TestRequest>>();
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddHandlers_WhenDirectIHandlerImplementation_ThenRegistersProcessor()
    {
        var services = new ServiceCollection();

        services.AddHandlers(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var processor = provider.GetService<IHandler<DirectRequest>>();

        Assert.NotNull(processor);
        Assert.IsType<DirectRequestProcessor>(processor);
    }

    [Fact]
    public void AddHandlers_WhenParameterlessIHandlerExists_ThenDoesNotRegisterIt()
    {
        var services = new ServiceCollection();

        services.AddHandlers(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var parameterless = provider.GetService<IHandler>();
        var concreteParameterless = provider.GetService<ParameterlessOnlyHandler>();

        Assert.Null(parameterless);
        Assert.Null(concreteParameterless);
    }

    [Fact]
    public void AddChainHandlers_WhenCalled_ThenRegistersChainHandlersFromAssembly()
    {
        var services = new ServiceCollection();

        services.AddChainHandlers(Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();
        var chain = provider.GetService<TestChainHandler>();
        Assert.NotNull(chain);
    }

    public record TestRequest : IRequest;

    public record DirectRequest : IRequest;

    public class TestHandler(IVerifier<TestRequest> verifier) : Handler<TestRequest>(verifier)
    {
        protected override Task HandleUseCaseAsync(TestRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public class TestChainHandler : ChainHandler<string>
    {
        public override Task ContinueAsync(string chain, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public class DirectRequestProcessor : IHandler<DirectRequest>
    {
        public Task HandleAsync(DirectRequest request, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    public class ParameterlessOnlyHandler : IHandler
    {
        public Task HandleAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
