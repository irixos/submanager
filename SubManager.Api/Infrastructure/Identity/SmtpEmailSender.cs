using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;

namespace SubManager.Api.Infrastructure.Identity;

public sealed class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger) : IEmailSender<ApplicationUser>
{
    private string? Host => configuration["Smtp:Host"];
    private string? Username => configuration["Smtp:Username"];
    private string? Password => configuration["Smtp:Password"];
    private string? FromAddress => string.IsNullOrWhiteSpace(configuration["Smtp:FromAddress"])
        ? Username
        : configuration["Smtp:FromAddress"];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(FromAddress);

    public Task SendConfirmationLinkAsync(
        ApplicationUser user,
        string email,
        string confirmationLink) => Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(
        ApplicationUser user,
        string email,
        string resetLink) => Task.CompletedTask;

    public async Task SendPasswordResetCodeAsync(
        ApplicationUser user,
        string email,
        string resetCode)
    {
        if (!IsConfigured)
            return;

        try
        {
            using var message = new MailMessage(FromAddress!, email)
            {
                Subject = "Reset your SubManager password",
                Body = $"Your SubManager password reset code is:\n\n{resetCode}\n\nIf you did not request this, you can ignore this email."
            };
            using var client = new SmtpClient(Host!, configuration.GetValue("Smtp:Port", 587))
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(Username, Password)
            };

            await client.SendMailAsync(message);
        }
        catch (Exception exception) when (exception is
                   SmtpException or InvalidOperationException or ArgumentException or FormatException)
        {
            logger.LogError(exception, "Failed to send a password reset email.");
        }
    }
}
