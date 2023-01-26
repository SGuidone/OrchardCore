using Microsoft.AspNetCore.Http;
using OrchardCore.Security;

namespace OrchardCore.Microsoft.Authentication.Settings
{
    public class MicrosoftAccountSettings : OAuthSettings
    {
        public string AppId { get; set; }

        public string AppSecret { get; set; }

        public PathString CallbackPath { get; set; }

        public bool SaveTokens { get; set; }
    }
}
