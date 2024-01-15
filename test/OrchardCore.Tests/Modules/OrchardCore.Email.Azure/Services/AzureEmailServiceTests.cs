using Microsoft.Extensions.Logging.Testing;

namespace OrchardCore.Email.Azure.Services.Tests;

public class AzureEmailServiceTests
{
    [Fact(Skip = "Configure the default sender and connection string for Email Communication Services (ECS) before run this test.")]
    public async Task SendEmailShouldSucceed()
    {
        // Arrange
        var emailOptions = Options.Create(new AzureEmailSettings
        {
            DefaultSender = "<<Sender>>",
            ConnectionString = "<<ConnectionString>>"
        });
        var logger = new FakeLogger<AzureEmailService>();
        var emailService = new AzureEmailService(
            emailOptions,
            logger,
            Mock.Of<IStringLocalizer<AzureEmailService>>(),
            new EmailAddressValidator());
        var message = new MailMessage
        {
            To = "test@orchardcore.net",
            Subject = "Orchard Core",
            Body = "This is a test message."
        };

        // Act
        var result = await emailService.SendAsync(message);

        // Assert
        Assert.True(result.Succeeded);
    }
}
