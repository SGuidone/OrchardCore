using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Liquid;
using OrchardCore.Modules;
using OrchardCore.Scripting;
using OrchardCore.Secrets.Models;
using OrchardCore.Secrets.Options;
using OrchardCore.Workflows.Helpers;
using OrchardCore.Workflows.Http.Activities;
using OrchardCore.Workflows.Http.Drivers;
using OrchardCore.Workflows.Http.Filters;
using OrchardCore.Workflows.Http.Handlers;
using OrchardCore.Workflows.Http.Liquid;
using OrchardCore.Workflows.Http.Models;
using OrchardCore.Workflows.Http.Scripting;
using OrchardCore.Workflows.Http.Services;
using OrchardCore.Workflows.Http.WorkflowContextProviders;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Workflows.Http
{
    [Feature("OrchardCore.Workflows.Http")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MvcOptions>(o =>
            {
                o.Filters.Add(typeof(WorkflowActionFilter));
            });

            services.AddLiquidFilter<SignalUrlFilter>("signal_url");

            services.AddScoped<IWorkflowTypeEventHandler, WorkflowTypeRoutesHandler>();
            services.AddScoped<IWorkflowHandler, WorkflowRoutesHandler>();

            services.AddSingleton<IWorkflowTypeRouteEntries, WorkflowTypeRouteEntries>();
            services.AddSingleton<IWorkflowInstanceRouteEntries, WorkflowInstanceRouteEntries>();
            services.AddSingleton<IGlobalMethodProvider, HttpMethodsProvider>();
            services.AddScoped<IWorkflowExecutionContextHandler, SignalWorkflowExecutionContextHandler>();

            services.AddActivity<HttpRequestEvent, HttpRequestEventDisplayDriver>();
            services.AddActivity<HttpRequestFilterEvent, HttpRequestFilterEventDisplayDriver>();
            services.AddActivity<HttpRedirectTask, HttpRedirectTaskDisplayDriver>();
            services.AddActivity<HttpRequestTask, HttpRequestTaskDisplayDriver>();
            services.AddActivity<HttpResponseTask, HttpResponseTaskDisplayDriver>();
            services.AddActivity<SignalEvent, SignalEventDisplayDriver>();

            services.AddSingleton<IGlobalMethodProvider, TokenMethodProvider>();
            services.AddScoped<IDisplayDriver<SecretBase>, HttpRequestEventSecretDisplayDriver>();
            services.AddScoped<IHttpRequestEventSecretService, HttpRequestEventSecretService>();
            services.Configure<SecretOptions>(options =>
            {
                options.SecretTypes.Add(typeof(HttpRequestEventSecret));
            });
        }

        public override void Configure(IApplicationBuilder app, IEndpointRouteBuilder routes, IServiceProvider serviceProvider)
        {
            routes.MapAreaControllerRoute(
                name: "HttpWorkflow",
                areaName: "OrchardCore.Workflows",
                pattern: "workflows/{action}",
                defaults: new { controller = "HttpWorkflow" }
            );

            routes.MapAreaControllerRoute(
                name: "InvokeWorkflow",
                areaName: "OrchardCore.Workflows",
                pattern: "workflows/invoke/{token}",
                defaults: new { controller = "HttpWorkflow", action = "Invoke" }
            );
        }
    }
}
