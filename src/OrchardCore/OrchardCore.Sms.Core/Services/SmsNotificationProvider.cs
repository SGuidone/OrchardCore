using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Notifications;
using OrchardCore.Users.Models;

namespace OrchardCore.Sms.Services;

public class SmsNotificationProvider : INotificationMethodProvider
{
    private readonly ISmsService _smsService;
    protected readonly IStringLocalizer S;

    public SmsNotificationProvider(
        ISmsService smsService,
        IStringLocalizer<SmsNotificationProvider> stringLocalizer)
    {
        _smsService = smsService;
        S = stringLocalizer;
    }

    public string Method => "SMS";

    public LocalizedString Name => S["SMS Notifications"];

    public async Task<bool> TrySendAsync(object notify, INotificationMessage message)
    {
        var user = notify as User;

        if (string.IsNullOrEmpty(user?.Email))
        {
            return false;
        }

        var mailMessage = new SmsMessage()
        {
            To = user.Email,
            Body = message.TextBody,
        };

        var result = await _smsService.SendAsync(mailMessage);

        return result.Succeeded;
    }
}
