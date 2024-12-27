using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotEmilu;

public static class DiContainer
{
    public static IServiceCollection AddDotEmilu(this IServiceCollection services, IConfiguration configuration)
        => services
            .Configure<ResultMessage>(message =>
            {
                var section = configuration.GetSection(ResultMessage.SectionKey);
                if (section.Exists())
                    section.Bind(message);
                else
                    ArgumentNullException.ThrowIfNull(section);
            })
            .AddScoped(typeof(IVerifier<>), typeof(Verifier<>))
            .AddScoped<IPresenter, Presenter>();
}