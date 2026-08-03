using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Features.Identity.Models;
using SubManager.Api.Application.Features.Identity.Services;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.Identity;

namespace SubManager.Api.Web;

public static class IdentityEndpoints
{
    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/setup-status",
            async (
                ApplicationDbContext db,
                SmtpEmailSender emailSender,
                CancellationToken cancellationToken) =>
                TypedResults.Ok(new SetupStatusResponse(
                    !await db.Users.AnyAsync(cancellationToken),
                    emailSender.IsConfigured)))
            .WithName("SetupStatus")
            .WithSummary("Get account setup and email availability");

        group.MapPost("/logout",
            async (SignInManager<ApplicationUser> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.NoContent();
            })
            .WithName("Logout")
            .WithSummary("Log out")
            .RequireAuthorization();

        group.MapPost("/change-email",
            async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> (
                ClaimsPrincipal principal,
                ChangeEmailRequest request,
                AccountService accountService,
                CancellationToken ct) =>
            {
                var result = await accountService.ChangeEmailAsync(principal, request, ct);
                if (result is null)
                    return TypedResults.Unauthorized();

                return result.Succeeded
                    ? TypedResults.NoContent()
                    : CreateValidationProblem(result);
            })
            .WithName("ChangeEmail")
            .WithSummary("Change the login email")
            .ProducesValidationProblem()
            .RequireAuthorization();

        group.MapPost("/delete-account",
            async Task<Results<NoContent, UnauthorizedHttpResult, ValidationProblem>> (
                ClaimsPrincipal principal,
                DeleteAccountRequest request,
                AccountService accountService,
                CancellationToken ct) =>
            {
                var result = await accountService.DeleteAsync(principal, request, ct);
                if (result is null)
                    return TypedResults.Unauthorized();

                return result.Succeeded
                    ? TypedResults.NoContent()
                    : CreateValidationProblem(result);
            })
            .WithName("DeleteAccount")
            .WithSummary("Delete the account and all application data")
            .ProducesValidationProblem()
            .RequireAuthorization();

        return group;
    }

    private static ValidationProblem CreateValidationProblem(IdentityResult result) =>
        TypedResults.ValidationProblem(result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray()));
}

public sealed record SetupStatusResponse(bool CanRegister, bool CanSendEmail);
