using Microsoft.Extensions.Configuration;
using OrchardCore.Email.Smtp;
using OrchardCore.Environment.Shell.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OrchardCoreBuilderExtensions
    {
        public static OrchardCoreBuilder ConfigureSmtpEmailSettings(this OrchardCoreBuilder builder)
        {
            builder.ConfigureServices((tenantServices, serviceProvider) =>
            {
                var configurationSection = serviceProvider.GetRequiredService<IShellConfiguration>().GetSection("OrchardCore_Email_Smtp");

                tenantServices.PostConfigure<SmtpEmailSettings>(settings => configurationSection.Bind(settings));
            });

            return builder;
        }
    }
}
