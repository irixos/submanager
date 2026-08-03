using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SubManager.Api.Infrastructure.Identity;

public static class AuthenticationValidationExtensions
{
    public static WebApplication UseValidUserAuthentication(this WebApplication app)
    {
        var securityStampClaimType = app.Services
            .GetRequiredService<IOptions<IdentityOptions>>()
            .Value.ClaimsIdentity.SecurityStampClaimType;

        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                context.GetEndpoint()?.Metadata.GetMetadata<IAuthorizeData>() is not null)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var securityStamp = context.User.FindFirstValue(securityStampClaimType);
                var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();

                if (userId is null ||
                    securityStamp is null ||
                    !await db.Users.AnyAsync(
                        user => user.Id == userId && user.SecurityStamp == securityStamp,
                        context.RequestAborted))
                {
                    await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }

            await next(context);
        });

        return app;
    }
}
