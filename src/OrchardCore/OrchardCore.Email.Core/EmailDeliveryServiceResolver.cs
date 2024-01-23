using System;
using OrchardCore.Email.Services;

namespace OrchardCore.Email;

public class EmailDeliveryServiceResolver : IEmailDeliveryServiceResolver
{
    private readonly IServiceProvider _serviceProvider;

    public EmailDeliveryServiceResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEmailDeliveryService Resolve(string name)
    {
        var emailDeliveryServiceDictionary = _serviceProvider.GetEmailDeliveryServiceDictionary();

        if (!emailDeliveryServiceDictionary.TryGetValue(name, out var emailDeliveryService))
        {
            emailDeliveryService = emailDeliveryServiceDictionary[EmailDeliveryServiceName.None];
        }

        return emailDeliveryService;
    }
}
