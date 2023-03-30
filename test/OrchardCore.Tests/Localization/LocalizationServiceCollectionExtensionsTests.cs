using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Localization;
using OrchardCore.Localization.Data;
using Xunit;

namespace OrchardCore.Tests.Localization
{
    public class LocalizationServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDataLocaliztion_RegisterRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddMemoryCache();
            services.AddSingleton<ILocalizationManager, LocalizationManager>();

            // Act
            services.AddDataLocalization();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var translationProvider = serviceProvider.GetService<IDataTranslationProvider>();
            var dataResourceManager = serviceProvider.GetService<DataResourceManager>();
            var dataLocalizerFactory = serviceProvider.GetService<IDataLocalizerFactory>();
            var dataLocalizer = serviceProvider.GetService<IDataLocalizer>();

            Assert.NotNull(translationProvider);
            Assert.NotNull(dataResourceManager);
            Assert.NotNull(dataLocalizerFactory);
            Assert.NotNull(dataLocalizer);
        }
    }
}
