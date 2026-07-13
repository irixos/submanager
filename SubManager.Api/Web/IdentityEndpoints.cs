using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.Identity;

namespace SubManager.Api.Web;

public static class IdentityEndpoints
{
    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/setup-status",
            async (ApplicationDbContext db, CancellationToken cancellationToken) =>
                TypedResults.Ok(new SetupStatusResponse(
                    !await db.Users.AnyAsync(cancellationToken))));

        group.MapPost("/logout",
            async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.NoContent();
            })
            .RequireAuthorization();

        return group;
    }
}

public sealed record SetupStatusResponse(bool CanRegister);
