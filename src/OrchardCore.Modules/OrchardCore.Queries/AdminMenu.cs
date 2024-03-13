using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Navigation;

namespace OrchardCore.Queries
{
    public class AdminMenu : INavigationProvider
    {
        protected readonly IStringLocalizer S;

        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!NavigationHelper.IsAdminMenu(name))
            {
                return Task.CompletedTask;
            }

            builder
                .Add(S["Search"], NavigationConstants.AdminMenuSearchPosition, search => search
                    .AddClass("search")
                    .Id("search")
                    .Add(S["Queries"], S["Queries"].PrefixPosition(), contentItems => contentItems
                        .Add(S["All queries"], "1", queries => queries
                        .Action("Index", "Admin", "OrchardCore.Queries")
                        .Permission(Permissions.ManageQueries)
                        .LocalNav()
                    )
                )
            );

            return Task.CompletedTask;
        }
    }
}
