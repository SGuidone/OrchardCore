using System.Text.Json.Nodes;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentTypes.Services;
using OrchardCore.ContentTypes.ViewModels;

namespace OrchardCore.DataLocalization.Services.Tests;

public class DataLocalizationProviderTests
{
    [Fact]
    public void ContentTypeDataLocalizationProvider_GetLocalizedStrings()
    {
        var contentDefinitionService = new Mock<IContentDefinitionService>();
        contentDefinitionService.Setup(cds => cds.GetTypesAsync())
            .ReturnsAsync(() => new List<EditTypeViewModel> {
                new() { DisplayName = "Article" },
                new() { DisplayName = "BlogPost" },
                new() { DisplayName = "News" }
            });
        var dataLocalizationProvider = new ContentTypeDataLocalizationProvider(contentDefinitionService.Object);
        var localizedStrings = dataLocalizationProvider.GetDescriptors();

        Assert.Equal(3, localizedStrings.Count());
        Assert.True(localizedStrings.All(s => s.Context == "Content Types"));
    }

    [Fact]
    public void ContentFieldDataLocalizationProvider_GetLocalizedStrings()
    {
        var contentDefinitionService = new Mock<IContentDefinitionService>();
        contentDefinitionService.Setup(cds => cds.GetTypesAsync())
            .ReturnsAsync(() => new List<EditTypeViewModel>
            {
                new() { DisplayName = "BlogPost", TypeDefinition = CreateContentTypeDefinition("BlogPost", "Blog Post", ["Title", "Body", "Author"]) },
                new() { DisplayName = "Person", TypeDefinition = CreateContentTypeDefinition("Person", "Person",  ["FirstName", "LastName"]) },
            });
        var dataLocalizationProvider = new ContentFieldDataLocalizationProvider(contentDefinitionService.Object);
        var localizedStrings = dataLocalizationProvider.GetDescriptors();

        Assert.Equal(5, localizedStrings.Count());
        Assert.True(localizedStrings.All(s => s.Context == "Content Fields"));
    }

    private static ContentTypeDefinition CreateContentTypeDefinition(string name, string displayName, string[] fields)
    {
        var contentPartFieldDefinitions = new List<ContentPartFieldDefinition>();
        var settings = new JsonObject();

        foreach (var field in fields)
        {
            contentPartFieldDefinitions.Add(new ContentPartFieldDefinition(new ContentFieldDefinition("TextField"), field, settings));
        }

        return new ContentTypeDefinition(
            name,
            displayName,
            new List<ContentTypePartDefinition> { new("Part", new ContentPartDefinition("Part", contentPartFieldDefinitions, settings), settings) },
            settings);
    }
}
