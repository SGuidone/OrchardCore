using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Email;
using OrchardCore.Users.Models;

namespace OrchardCore.Notifications.Services;

public class EmailNotificationProvider : INotificationMethodProvider
{
    private readonly ISmtpService _smtpService;
    protected readonly IStringLocalizer S;

    public EmailNotificationProvider(
        ISmtpService smtpService,
        IStringLocalizer<EmailNotificationProvider> stringLocalizer)
    {
        _smtpService = smtpService;
        S = stringLocalizer;
    }

    public string Method => "Email";

    public LocalizedString Name => S["Email Notifications"];

    public async Task<bool> TrySendAsync(object notify, INotificationMessage message)
    {
        var user = notify as User;

        if (string.IsNullOrEmpty(user?.Email))
        {
            return false;
        }

        var mailMessage = new MailMessage()
        {
            To = user.Email,
            Subject = message.Summary,
            Content = new MailMessageBody()
        };
        if (message.IsHtmlPreferred && !string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            mailMessage.Content.Html = message.HtmlBody;
        }
        else
        {
            mailMessage.Content.Text = message.TextBody;
        }

        var result = await _smtpService.SendAsync(mailMessage);

        return result.Succeeded;
    }
}
