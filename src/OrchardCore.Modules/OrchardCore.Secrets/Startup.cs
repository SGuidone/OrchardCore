using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrchardCore.Admin;
using OrchardCore.Deployment;
using OrchardCore.DisplayManagement;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Mvc.Core.Utilities;
using OrchardCore.Navigation;
using OrchardCore.Recipes;
using OrchardCore.Secrets.Controllers;
using OrchardCore.Secrets.Deployment;
using OrchardCore.Secrets.Drivers;
using OrchardCore.Secrets.Models;
using OrchardCore.Secrets.Options;
using OrchardCore.Secrets.Recipes;
using OrchardCore.Secrets.Services;
using OrchardCore.Security.Permissions;

namespace OrchardCore.Secrets
{
    public class Startup : StartupBase
    {
        private readonly AdminOptions _adminOptions;

        public Startup(IOptions<AdminOptions> adminOptions) => _adminOptions = adminOptions.Value;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IPermissionProvider, Permissions>();
            services.AddScoped<INavigationProvider, AdminMenu>();

            services.AddScoped<IDisplayManager<SecretBase>, DisplayManager<SecretBase>>();
            services.AddScoped<IDisplayDriver<SecretBase>, TextSecretDisplayDriver>();
            services.AddScoped<IDisplayDriver<SecretBase>, RsaSecretDisplayDriver>();

            services.AddSingleton<ISecretService, SecretService>();
            services.AddSingleton<ISecretProtectionProvider, SecretProtectionProvider>();
            services.AddSingleton<ISecretStore, DatabaseSecretStore>();

            services.Configure<SecretOptions>(options =>
            {
                options.SecretTypes.Add(typeof(TextSecret));
                options.SecretTypes.Add(typeof(RSASecret));
            });

            services.AddSingleton<SecretBindingsManager>();
            services.AddSingleton<SecretsDocumentManager>();

            services.AddRecipeExecutionStep<SecretsRecipeStep>();
            services.AddTransient<IDeploymentSource, AllSecretsRsaDeploymentSource>();
            services.AddSingleton<IDeploymentStepFactory>(new DeploymentStepFactory<AllSecretsRsaDeploymentStep>());
            services.AddScoped<IDisplayDriver<DeploymentStep>, AllSecretsRsaDeploymentStepDriver>();
        }

        public override void Configure(IApplicationBuilder builder, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            var secretControllerName = typeof(AdminController).ControllerName();

            routes.MapAreaControllerRoute(
                name: "Secrets.Index",
                areaName: "OrchardCore.Secrets",
                pattern: _adminOptions.AdminUrlPrefix + "/Secrets",
                defaults: new { controller = secretControllerName, action = nameof(AdminController.Index) }
            );

            routes.MapAreaControllerRoute(
                name: "Secrets.Create",
                areaName: "OrchardCore.Secrets",
                pattern: _adminOptions.AdminUrlPrefix + "/Secrets/Create",
                defaults: new { controller = secretControllerName, action = nameof(AdminController.Create) }
            );

            routes.MapAreaControllerRoute(
                name: "Secrets.Edit",
                areaName: "OrchardCore.Secrets",
                pattern: _adminOptions.AdminUrlPrefix + "/Secrets/Edit/{name}",
                defaults: new { controller = secretControllerName, action = nameof(AdminController.Edit) }
            );
        }
    }

    [Feature("OrchardCore.Secrets.ConfigurationSecretStore")]
    public class ConfigurationSecretStoreStartup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ISecretStore, ConfigurationSecretStore>();
        }
    }
}
