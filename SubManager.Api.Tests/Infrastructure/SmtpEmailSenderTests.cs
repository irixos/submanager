using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SubManager.Api.Infrastructure.Identity;
using Xunit;

namespace SubManager.Api.Tests.Infrastructure;

public sealed class SmtpEmailSenderTests
{
    [Fact]
    public async Task SendPasswordResetCodeAsync_InvalidConfiguration_DoesNotExposeFailure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "smtp.invalid",
                ["Smtp:Username"] = "owner@example.com",
                ["Smtp:Password"] = "password",
                ["Smtp:FromAddress"] = "@"
            })
            .Build();
        var sender = new SmtpEmailSender(
            configuration,
            NullLogger<SmtpEmailSender>.Instance);

        var exception = await Record.ExceptionAsync(() => sender.SendPasswordResetCodeAsync(
            new ApplicationUser(),
            "owner@example.com",
            "reset-code"));

        Assert.True(sender.IsConfigured);
        Assert.Null(exception);
    }
}
