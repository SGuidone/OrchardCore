using OrchardCore.Documents;
using OrchardCore.Secrets;
using OrchardCore.Secrets.Models;
using OrchardCore.Secrets.Services;

namespace OrchardCore.Tests.Modules.OrchardCore.Secrets;

public class SecretServiceTests
{
    [Fact]
    public async Task ShouldGetTextSecret()
    {
        var store = Mock.Of<ISecretStore>();

        var textSecret = new TextSecret()
        {
            Text = "myemailpassword"
        };

        Mock.Get(store).Setup(s => s.GetSecretAsync("email", typeof(TextSecret))).ReturnsAsync(textSecret);

        var documentManager = Mock.Of<IDocumentManager<SecretBindingsDocument>>();

        Mock.Get(documentManager).Setup(m => m.GetOrCreateImmutableAsync(It.IsAny<Func<Task<SecretBindingsDocument>>>()))
            .ReturnsAsync(() =>
            {
                var document = new SecretBindingsDocument();
                document.SecretBindings["email"] = new SecretBinding() { Name = "email" };
                return document;
            });

        var options = new SecretsOptions();
        options.SecretTypes.Add(typeof(TextSecret));
        var secretsOptions = Options.Create(options);

        var coordinator = new DefaultSecretCoordinator(
            new SecretBindingsManager(documentManager),
            new[] { store },
            secretsOptions);

        var service = new SecretService<TextSecret>(coordinator);
        var secret = await service.GetSecretAsync("email");

        Assert.Equal(secret, textSecret);
    }
}
