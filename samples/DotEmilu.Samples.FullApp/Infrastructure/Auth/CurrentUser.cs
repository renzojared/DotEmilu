using System.Security.Claims;
using DotEmilu.Samples.Domain.Contracts;

namespace DotEmilu.Samples.FullApp.Infrastructure.Auth;

/// <summary>
/// Resolves the current user from HTTP context claims.
/// Falls back to a demo Guid when no authentication is configured.
/// </summary>
public class CurrentUser(IHttpContextAccessor httpContextAccessor) : IAppUserContext
{
    private static readonly Guid DemoUserId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    public Guid Id
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && Guid.TryParse(claim.Value, out var userId)
                ? userId
                : DemoUserId;
        }
    }
}
