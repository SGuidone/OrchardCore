using System;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore.Deployment.Remote.Models;
using OrchardCore.Documents;
using OrchardCore.Secrets;
using OrchardCore.Secrets.Models;
using OrchardCore.Secrets.Services;

namespace OrchardCore.Deployment.Remote.Services
{
    public class RemoteInstanceService
    {
        private readonly IDocumentManager<RemoteInstanceList> _documentManager;
        private readonly ISecretService _secretService;

        public RemoteInstanceService(IDocumentManager<RemoteInstanceList> documentManager, ISecretService secretService)
        {
            _documentManager = documentManager;
            _secretService = secretService;
        }

        /// <summary>
        /// Loads the remote instances document from the store for updating and that should not be cached.
        /// </summary>
        public Task<RemoteInstanceList> LoadRemoteInstanceListAsync() => _documentManager.GetOrCreateMutableAsync();

        /// <summary>
        /// Gets the remote instances document from the cache for sharing and that should not be updated.
        /// </summary>
        public Task<RemoteInstanceList> GetRemoteInstanceListAsync() => _documentManager.GetOrCreateImmutableAsync();

        public async Task<RemoteInstance> LoadRemoteInstanceAsync(string id)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();
            return FindRemoteInstance(remoteInstanceList, id);
        }

        public async Task<RemoteInstance> GetRemoteInstanceAsync(string id)
        {
            var remoteInstanceList = await GetRemoteInstanceListAsync();
            return FindRemoteInstance(remoteInstanceList, id);
        }

        public async Task DeleteRemoteInstanceAsync(string id)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();

            var remoteInstance = FindRemoteInstance(remoteInstanceList, id);
            if (remoteInstance is null)
            {
                return;
            }

            remoteInstanceList.RemoteInstances.Remove(remoteInstance);
            await _documentManager.UpdateAsync(remoteInstanceList);
        }

        public async Task CreateRemoteInstanceAsync(string name, string url, string clientName, string apiKey)
        {
            var rsaEncryptionSecret = await _secretService.GetOrCreateSecretAsync<RSASecret>(
                name: $"OrchardCore.Deployment.Remote.RsaEncryptionSecret.{clientName}",
                configure: secret => RSAGenerator.ConfigureRSASecretKeys(secret, RSAKeyType.Public));

            var rsaSigningSecret = await _secretService.GetOrCreateSecretAsync<RSASecret>(
                name: $"OrchardCore.Deployment.Remote.RsaSigningSecret.{clientName}",
                configure: secret => RSAGenerator.ConfigureRSASecretKeys(secret, RSAKeyType.PublicPrivatePair));

            var apiSecret = await _secretService.GetOrCreateSecretAsync<TextSecret>(
                name: $"OrchardCore.Deployment.Remote.ApiKey.{clientName}",
                configure: secret => secret.Text = apiKey);

            var remoteInstanceList = await LoadRemoteInstanceListAsync();
            var remoteInstance = new RemoteInstance
            {
                Id = Guid.NewGuid().ToString("n"),
                Name = name,
                Url = url,
                ClientName = clientName,
            };

            remoteInstanceList.RemoteInstances.Add(remoteInstance);
            await _documentManager.UpdateAsync(remoteInstanceList);
        }

        public Task UpdateRemoteInstanceAsync(RemoteInstance remoteInstance, string apiKey) =>
            UpdateRemoteInstanceAsync(remoteInstance.Id, remoteInstance.Name, remoteInstance.Url, remoteInstance.ClientName, apiKey);

        public async Task UpdateRemoteInstanceAsync(string id, string name, string url, string clientName, string apiKey)
        {
            var remoteInstanceList = await LoadRemoteInstanceListAsync();

            var remoteInstance = FindRemoteInstance(remoteInstanceList, id);
            if (remoteInstance is null)
            {
                return;
            }

            var rsaEncryptionSecret = await _secretService.GetOrCreateSecretAsync<RSASecret>(
                name: $"OrchardCore.Deployment.Remote.RsaEncryptionSecret.{clientName}",
                configure: secret => RSAGenerator.ConfigureRSASecretKeys(secret, RSAKeyType.Public),
                sourceName: $"OrchardCore.Deployment.Remote.RsaEncryptionSecret.{remoteInstance.ClientName}");

            var rsaSigningSecret = await _secretService.GetOrCreateSecretAsync<RSASecret>(
                name: $"OrchardCore.Deployment.Remote.RsaSigningSecret.{clientName}",
                configure: secret => RSAGenerator.ConfigureRSASecretKeys(secret, RSAKeyType.PublicPrivatePair),
                sourceName: $"OrchardCore.Deployment.Remote.RsaSigningSecret.{remoteInstance.ClientName}");

            var apiKeySecret = await _secretService.GetOrCreateSecretAsync<TextSecret>(
                name: $"OrchardCore.Deployment.Remote.ApiKey.{clientName}",
                configure: secret => secret.Text = apiKey,
                sourceName: $"OrchardCore.Deployment.Remote.ApiKey.{remoteInstance.ClientName}");

            if (apiKeySecret.Text != apiKey)
            {
                apiKeySecret.Text = apiKey;
                await _secretService.UpdateSecretAsync(apiKeySecret);
            }

            remoteInstance.Name = name;
            remoteInstance.Url = url;
            remoteInstance.ClientName = clientName;

            await _documentManager.UpdateAsync(remoteInstanceList);
        }

        private static RemoteInstance FindRemoteInstance(RemoteInstanceList remoteInstanceList, string id) =>
            remoteInstanceList.RemoteInstances.FirstOrDefault(remote => string.Equals(remote.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}
