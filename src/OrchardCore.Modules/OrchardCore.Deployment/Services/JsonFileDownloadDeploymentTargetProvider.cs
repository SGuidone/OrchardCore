using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace OrchardCore.Deployment
{
    public class JsonFileDownloadDeploymentTargetProvider : IDeploymentTargetProvider
    {
        protected readonly IStringLocalizer S;

        public JsonFileDownloadDeploymentTargetProvider(IStringLocalizer<FileDownloadDeploymentTargetProvider> stringLocalizer)
        {
            S = stringLocalizer;
        }

        public Task<IEnumerable<DeploymentTarget>> GetDeploymentTargetsAsync()
        {
            return Task.FromResult<IEnumerable<DeploymentTarget>>(
                new[] {
                    new DeploymentTarget(
                        name: S["Json File Download"],
                        description: S["Download a Json deployment plan locally."],
                        route: new RouteValueDictionary(new
                        {
                            area = "OrchardCore.Deployment",
                            controller = "ExportFile",
                            action = "ExecuteJson"
                        })
                    )
                }
            );
        }
    }
}
