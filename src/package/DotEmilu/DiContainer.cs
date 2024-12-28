using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu;

public static class DiContainer
{
    public static IServiceCollection AddDotEmilu(this IServiceCollection services, IConfiguration configuration)
        => services
            .Configure<ResultMessage>(message =>
                configuration.GetRequiredSection(ResultMessage.SectionKey).Bind(message))
            .AddScoped(typeof(IVerifier<>), typeof(Verifier<>))
            .AddScoped<IPresenter, Presenter>();
}