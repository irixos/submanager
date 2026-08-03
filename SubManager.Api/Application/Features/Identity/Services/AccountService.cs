using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Features.Identity.Models;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.Identity;

namespace SubManager.Api.Application.Features.Identity.Services;

public sealed class AccountService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
{
    public async Task<IdentityResult?> ChangeEmailAsync(
        ClaimsPrincipal principal,
        ChangeEmailRequest request,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return null;

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return IdentityResult.Failed(userManager.ErrorDescriber.PasswordMismatch());

        var newEmail = request.NewEmail.Trim();
        if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            return IdentityResult.Success;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var result = await userManager.SetEmailAsync(user, newEmail);
        if (!result.Succeeded)
            return result;

        result = await userManager.SetUserNameAsync(user, newEmail);
        if (!result.Succeeded)
            return result;

        user.EmailConfirmed = true;
        result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return result;

        result = await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
            return result;

        await transaction.CommitAsync(ct);
        await signInManager.RefreshSignInAsync(user);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult?> DeleteAsync(
        ClaimsPrincipal principal,
        DeleteAccountRequest request,
        CancellationToken ct)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return null;

        if (!await userManager.CheckPasswordAsync(user, request.Password))
            return IdentityResult.Failed(userManager.ErrorDescriber.PasswordMismatch());

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Channels.ExecuteDeleteAsync(ct);
        await db.Categories.ExecuteDeleteAsync(ct);
        await db.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);

        await signInManager.SignOutAsync();
        return IdentityResult.Success;
    }
}
