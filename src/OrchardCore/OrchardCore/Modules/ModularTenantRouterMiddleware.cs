using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrchardCore.Clusters;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Scope;

namespace OrchardCore.Modules
{
    /// <summary>
    /// Handles a request by forwarding it to the tenant specific pipeline.
    /// It also initializes the middlewares for the requested tenant on the first request.
    /// </summary>
    public class ModularTenantRouterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFeatureCollection _features;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        public ModularTenantRouterMiddleware(
            RequestDelegate next,
            IFeatureCollection features,
            ILogger<ModularTenantRouterMiddleware> logger)
        {
            _next = next;
            _features = features;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // Check if this instance is used as a clusters proxy.
            if (httpContext.TryGetClusterFeature(out _))
            {
                // Bypass the routing middleware.
                await _next(httpContext);
                return;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Begin Routing Request");
            }

            var shellContext = ShellScope.Context;

            // Define a PathBase for the current request that is the RequestUrlPrefix.
            // This will allow any view to reference ~/ as the tenant's base url.
            // Because IIS or another middleware might have already set it, we just append the tenant prefix value.
            if (!String.IsNullOrEmpty(shellContext.Settings.RequestUrlPrefix))
            {
                PathString prefix = "/" + shellContext.Settings.RequestUrlPrefix;
                httpContext.Request.PathBase += prefix;
                httpContext.Request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase, out var remainingPath);
                httpContext.Request.Path = remainingPath;
            }

            // Do we need to rebuild the pipeline?
            if (shellContext.Pipeline is null)
            {
                await InitializePipelineAsync(shellContext);
            }

            // Update the last request time (done atomically).
            shellContext.LastRequestTimeUtc = DateTime.UtcNow;

            await shellContext.Pipeline.Invoke(httpContext);
        }

        private async Task InitializePipelineAsync(ShellContext shellContext)
        {
            var semaphore = _semaphores.GetOrAdd(shellContext.Settings.Name, _ => new SemaphoreSlim(1));

            // Building a pipeline for a given shell can't be done by two requests.
            await semaphore.WaitAsync();

            try
            {
                shellContext.Pipeline ??= BuildTenantPipeline();
            }
            finally
            {
                semaphore.Release();
            }
        }

        // Build the middleware pipeline for the current tenant.
        private IShellPipeline BuildTenantPipeline()
        {
            var appBuilder = new ApplicationBuilder(ShellScope.Context.ServiceProvider, _features);

            // Create a nested pipeline to configure the tenant middleware pipeline.
            var startupFilters = appBuilder.ApplicationServices.GetService<IEnumerable<IStartupFilter>>();

            var shellPipeline = new ShellRequestPipeline();

            Action<IApplicationBuilder> configure = builder =>
            {
                ConfigureTenantPipeline(builder);
            };

            foreach (var filter in startupFilters.Reverse())
            {
                configure = filter.Configure(configure);
            }

            configure(appBuilder);

            shellPipeline.Next = appBuilder.Build();

            return shellPipeline;
        }

        private static void ConfigureTenantPipeline(IApplicationBuilder appBuilder)
        {
            var startups = appBuilder.ApplicationServices.GetServices<IStartup>();

            // IStartup instances are ordered by module dependency with a 'ConfigureOrder' of 0 by default.
            // OrderBy performs a stable sort so order is preserved among equal 'ConfigureOrder' values.
            startups = startups.OrderBy(s => s.ConfigureOrder);

            appBuilder.UseRouting().UseEndpoints(routes =>
            {
                foreach (var startup in startups)
                {
                    startup.Configure(appBuilder, routes, ShellScope.Services);
                }
            });
        }
    }
}
