using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotEmilu.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for configuring Entity Framework Core dependencies.
/// </summary>
public static class DiContainer
{
    /// <param name="services">The service collection.</param>
    extension(IServiceCollection services)
    {
        /// <summary>Registers the soft delete interceptor.</summary>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddSoftDeleteInterceptor()
            => services.AddScoped<ISaveChangesInterceptor, SoftDeleteInterceptor>();

        /// <summary>Registers a specific auditable entity interceptor and its required context user implementation.</summary>
        /// <typeparam name="TContextUser">The implementation type of the context user.</typeparam>
        /// <typeparam name="TUserKey">The user key type (e.g., Guid or long).</typeparam>
        /// <returns>The updated service collection.</returns>
        public IServiceCollection AddAuditableEntityInterceptor<TContextUser, TUserKey>()
            where TContextUser : class, IContextUser<TUserKey>
            where TUserKey : struct
        {
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddScoped<IContextUser<TUserKey>, TContextUser>();
            return services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor<TUserKey>>();
        }

        /// <summary>Scans an assembly to automatically register all auditable entity interceptors and context user implementations.</summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>The updated service collection.</returns>
        /// <exception cref="ArgumentException">Thrown when multiple implementations for the same user key type are found.</exception>
        public IServiceCollection AddAuditableEntityInterceptors(Assembly assembly)
        {
            services.TryAddSingleton(TimeProvider.System);

            var groupedByUserKeyType = GetContextUserImplementations(assembly)
                .Select(t => new
                {
                    Implementation = t,
                    Interface = t.GetInterfaces().First(IsContextUserInterface),
                    UserKeyType = t.GetInterfaces().First(IsContextUserInterface).GetGenericArguments()[0]
                })
                .GroupBy(x => x.UserKeyType)
                .ToList();

            if (groupedByUserKeyType.Any(g => g.Count() > 1))
            {
                var duplicateKeys = string.Join(", ", groupedByUserKeyType
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key.Name));
                throw new ArgumentException(
                    $"There are multiple implementations of IContextUser<T> for the following key types: {duplicateKeys}");
            }

            foreach (var group in groupedByUserKeyType)
            {
                var contextUser = group.First();
                services.TryAddScoped(contextUser.Interface, contextUser.Implementation);
                var userKeyType = group.Key;
                var auditableInterceptorType = typeof(AuditableEntityInterceptor<>).MakeGenericType(userKeyType);
                services.AddScoped(typeof(ISaveChangesInterceptor), auditableInterceptorType);
            }

            return services;

            static IEnumerable<Type> GetContextUserImplementations(Assembly assembly)
                => assembly
                    .GetTypes()
                    .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                                t.GetInterfaces().Any(IsContextUserInterface));

            static bool IsContextUserInterface(Type i)
                => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IContextUser<>);
        }
    }
}