using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using OrchardCore.Admin;
using OrchardCore.Deployment.Core.Services;
using OrchardCore.Deployment.Remote.Services;
using OrchardCore.Deployment.Remote.ViewModels;
using OrchardCore.Deployment.Services;
using OrchardCore.DisplayManagement.Notify;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Recipes.Models;
using OrchardCore.Secrets;
using OrchardCore.Secrets.Models;
using YesSql;

namespace OrchardCore.Deployment.Remote.Controllers
{
    [Admin]
    public class ExportRemoteInstanceController : Controller
    {
        private static readonly HttpClient _httpClient = new();

        private readonly RemoteInstanceService _service;
        private readonly IDeploymentManager _deploymentManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly ISession _session;
        private readonly ISecretService _secretService;
        private readonly INotifier _notifier;
        protected readonly IHtmlLocalizer H;

        public ExportRemoteInstanceController(
            RemoteInstanceService service,
            IAuthorizationService authorizationService,
            IDeploymentManager deploymentManager,
            ISecretService secretService,
            ISession session,
            INotifier notifier,
            IHtmlLocalizer<ExportRemoteInstanceController> localizer)
        {
            _service = service;
            _authorizationService = authorizationService;
            _deploymentManager = deploymentManager;
            _secretService = secretService;
            _session = session;
            _notifier = notifier;
            H = localizer;
        }

        [HttpPost]
        public async Task<IActionResult> Execute(long id, string remoteInstanceId, string returnUrl)
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.Export))
            {
                return Forbid();
            }

            var deploymentPlan = await _session.GetAsync<DeploymentPlan>(id);

            if (deploymentPlan == null)
            {
                return NotFound();
            }

            var remoteInstance = await _service.GetRemoteInstanceAsync(remoteInstanceId);
            if (remoteInstance is null)
            {
                return NotFound();
            }

            string archiveFileName;
            var filename = deploymentPlan.Name.ToSafeName() + ".zip";

            using (var fileBuilder = new TemporaryFileBuilder())
            {
                archiveFileName = PathExtensions.Combine(Path.GetTempPath(), filename);

                var deploymentPlanResult = new DeploymentPlanResult(
                    fileBuilder,
                    new RecipeDescriptor(),
                    $"OrchardCore.Deployment.Remote.RsaEncryptionSecret.{remoteInstance.ClientName}",
                    $"OrchardCore.Deployment.Remote.RsaSigningSecret.{remoteInstance.ClientName}");

                await _deploymentManager.ExecuteDeploymentPlanAsync(deploymentPlan, deploymentPlanResult);

                if (System.IO.File.Exists(archiveFileName))
                {
                    System.IO.File.Delete(archiveFileName);
                }

                ZipFile.CreateFromDirectory(fileBuilder.Folder, archiveFileName);
            }

            HttpResponseMessage response;

            try
            {
                using (var requestContent = new MultipartFormDataContent())
                {
                    requestContent.Add(new StreamContent(
                        new FileStream(
                            archiveFileName,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite,
                            1,
                            FileOptions.Asynchronous | FileOptions.SequentialScan)),
                        nameof(ImportViewModel.Content),
                        Path.GetFileName(archiveFileName));

                    requestContent.Add(new StringContent(remoteInstance.ClientName), nameof(ImportViewModel.ClientName));

                    var secret = await _secretService.GetOrCreateSecretAsync<TextSecret>(
                        $"OrchardCore.Deployment.Remote.ApiKey.{remoteInstance.ClientName}");

                    requestContent.Add(new StringContent(secret.Text), nameof(ImportViewModel.ApiKey));

                    response = await _httpClient.PostAsync(remoteInstance.Url, requestContent);
                }

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    await _notifier.SuccessAsync(H["Deployment executed successfully."]);
                }
                else
                {
                    await _notifier.ErrorAsync(H["An error occurred while sending the deployment to the remote instance: \"{0} ({1})\"", response.ReasonPhrase, (int)response.StatusCode]);
                }
            }
            finally
            {
                System.IO.File.Delete(archiveFileName);
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return this.LocalRedirect(returnUrl, true);
            }

            return RedirectToAction("Display", "DeploymentPlan", new { area = "OrchardCore.Deployment", id });
        }
    }
}
