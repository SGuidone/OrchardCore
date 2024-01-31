using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Configuration;
using OrchardCore.Environment.Shell.Configuration.Internal;
using OrchardCore.Shells.Database.Extensions;
using OrchardCore.Shells.Database.Models;
using YesSql;

namespace OrchardCore.Shells.Database.Configuration
{
    public class DatabaseShellConfigurationSources : IShellConfigurationSources
    {
        private readonly DatabaseShellsStorageOptions _options;
        private readonly IShellContextFactory _shellContextFactory;
        private readonly string _container;

        public DatabaseShellConfigurationSources(
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            IShellContextFactory shellContextFactory,
            IOptions<ShellOptions> shellOptions)

        {
            _options = configuration
                .GetSection("OrchardCore")
                .GetSectionCompat("OrchardCore_Shells_Database")
                .Get<DatabaseShellsStorageOptions>()
                ?? new DatabaseShellsStorageOptions();

            _shellContextFactory = shellContextFactory;

            _container = Path.Combine(shellOptions.Value.ShellsApplicationDataPath, shellOptions.Value.ShellsContainerName);
        }

        public async Task AddSourcesAsync(string tenant, IConfigurationBuilder builder)
        {
            JObject configurations = null;

            await using var context = await _shellContextFactory.GetDatabaseContextAsync(_options);
            await (await context.CreateScopeAsync()).UsingServiceScopeAsync(async scope =>
            {
                var session = scope.ServiceProvider.GetRequiredService<ISession>();

                var document = await session.Query<DatabaseShellConfigurations>().FirstOrDefaultAsync();

                if (document is not null)
                {
                    configurations = document.ShellConfigurations;
                }
                else
                {
                    document = new DatabaseShellConfigurations();
                    configurations = [];
                }

                if (!configurations.ContainsKey(tenant))
                {
                    if (!_options.MigrateFromFiles || !await TryMigrateFromFileAsync(tenant, configurations))
                    {
                        return;
                    }

                    document.ShellConfigurations = configurations;

                    await session.SaveAsync(document, checkConcurrency: true);
                }
            });

            var configuration = configurations.GetValue(tenant) as JObject;
            if (configuration is not null)
            {
                var configurationString = await configuration.ToStringAsync(Formatting.None);
                builder.AddTenantJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(configurationString)));
            }
        }

        public async Task SaveAsync(string tenant, IDictionary<string, string> data)
        {
            await using var context = await _shellContextFactory.GetDatabaseContextAsync(_options);
            await (await context.CreateScopeAsync()).UsingServiceScopeAsync(async scope =>
            {
                var session = scope.ServiceProvider.GetRequiredService<ISession>();

                var document = await session.Query<DatabaseShellConfigurations>().FirstOrDefaultAsync();

                JObject configurations;
                if (document is not null)
                {
                    configurations = document.ShellConfigurations;
                }
                else
                {
                    document = new DatabaseShellConfigurations();
                    configurations = [];
                }

                var configData = await (configurations
                    .GetValue(tenant) as JObject)
                    .ToConfigurationDataAsync();

                foreach (var key in data.Keys)
                {
                    if (data[key] is not null)
                    {
                        configData[key] = data[key];
                    }
                    else
                    {
                        configData.Remove(key);
                    }
                }

                configurations[tenant] = configData.ToJObject();
                document.ShellConfigurations = configurations;

                await session.SaveAsync(document, checkConcurrency: true);
            });
        }

        public async Task RemoveAsync(string tenant)
        {
            await using var context = await _shellContextFactory.GetDatabaseContextAsync(_options);
            await (await context.CreateScopeAsync()).UsingServiceScopeAsync(async scope =>
            {
                var session = scope.ServiceProvider.GetRequiredService<ISession>();

                var document = await session.Query<DatabaseShellConfigurations>().FirstOrDefaultAsync();
                if (document is not null)
                {
                    document.ShellConfigurations.Remove(tenant);
                    await session.SaveAsync(document, checkConcurrency: true);
                }
            });
        }

        private async Task<bool> TryMigrateFromFileAsync(string tenant, JObject configurations)
        {
            var tenantFolder = Path.Combine(_container, tenant);
            var appsettings = Path.Combine(tenantFolder, "appsettings.json");

            if (!File.Exists(appsettings))
            {
                return false;
            }

            using var file = File.OpenText(appsettings);
            var configuration = await file.ReadToEndAsync();

            if (configuration is not null)
            {
                configurations[tenant] = JObject.Parse(configuration);
            }

            return true;
        }
    }
}
