using Microsoft.AspNetCore.Identity;
using SubManager.Api.Infrastructure.Identity;

namespace SubManager.Api.Web;

public static class IdentityEndpoints
{
    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/logout",
            async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.NoContent();
            });

        return group;
    }
}
