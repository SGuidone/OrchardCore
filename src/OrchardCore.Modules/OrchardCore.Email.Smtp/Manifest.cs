using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "SMTP Email",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "Provides email settings configuration and a default email service based on SMTP",
    Dependencies = ["OrchardCore.Email"],
    Category = "Messaging"
)]
