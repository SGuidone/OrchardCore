using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.Secrets.Services;

public class DefaultSecretCoordinator : ISecretCoordinator
{
    private readonly SecretBindingsManager _secretBindingsManager;
    private readonly IReadOnlyCollection<SecretStoreDescriptor> _secretStoreDescriptors;
    private readonly IEnumerable<ISecretStore> _stores;

    private readonly Dictionary<string, ISecretFactory> _factories = new();

    public DefaultSecretCoordinator(
        SecretBindingsManager secretBindingsManager,
        IEnumerable<ISecretStore> secretStores,
        IOptions<SecretsOptions> secretsOptions)
    {
        _secretBindingsManager = secretBindingsManager;

        _secretStoreDescriptors = secretStores.Select(store => new SecretStoreDescriptor
        {
            Name = store.Name,
            IsReadOnly = store.IsReadOnly,
            DisplayName = store.DisplayName,
        })
            .ToArray();

        _stores = secretStores;

        foreach (var type in secretsOptions.Value.SecretTypes)
        {
            var factoryType = typeof(SecretFactory<>).MakeGenericType(type);
            var factory = (ISecretFactory)Activator.CreateInstance(factoryType);
            _factories[type.Name] = factory;
        }
    }

    public Secret CreateSecret(string typeName)
    {
        if (!_factories.TryGetValue(typeName, out var factory) || !typeof(Secret).IsAssignableFrom(factory.Type))
        {
            throw new ArgumentException($"The type should be configured and implement '{nameof(Secret)}'.", nameof(typeName));
        }

        return factory.Create();
    }

    public async Task<Secret> GetSecretAsync(SecretBinding binding)
    {
        if (!_factories.TryGetValue(binding.Type, out var factory) ||
            !typeof(Secret).IsAssignableFrom(factory.Type))
        {
            return null;
        }

        var secretStore = _stores.FirstOrDefault(store => String.Equals(store.Name, binding.Store, StringComparison.OrdinalIgnoreCase));
        if (secretStore is null)
        {
            return null;
        }

        var secret = await secretStore.GetSecretAsync(binding.Name, factory.Type);
        if (secret is null)
        {
            secret = factory.Create();
            secret.Name = binding.Name;
        }

        return secret;
    }

    public async Task<Secret> GetSecretAsync(string key, Type type)
    {
        if (!_factories.TryGetValue(type.Name, out var factory) || !typeof(Secret).IsAssignableFrom(factory.Type))
        {
            throw new ArgumentException($"The type should be configured and implement '{nameof(Secret)}'.", nameof(type));
        }

        var bindings = await GetSecretBindingsAsync();
        if (!bindings.TryGetValue(key, out var binding))
        {
            return null;
        }

        var secretStore = _stores.FirstOrDefault(store => String.Equals(store.Name, binding.Store, StringComparison.OrdinalIgnoreCase));
        if (secretStore is null)
        {
            return null;
        }

        var secret = await secretStore.GetSecretAsync(binding.Name, factory.Type);
        if (secret is null)
        {
            secret = factory.Create();
            secret.Name = binding.Name;
        }

        return secret;
    }

    public TSecret CreateSecret<TSecret>() where TSecret : Secret, new()
        => CreateSecret(typeof(TSecret).Name) as TSecret;

    public async Task<TSecret> GetSecretAsync<TSecret>(string key) where TSecret : Secret, new()
        => (await GetSecretAsync(key, typeof(TSecret))) as TSecret;

    public async Task<IDictionary<string, SecretBinding>> GetSecretBindingsAsync()
    {
        var secretsDocument = await _secretBindingsManager.GetSecretBindingsDocumentAsync();
        return secretsDocument.SecretBindings;
    }

    public async Task<IDictionary<string, SecretBinding>> LoadSecretBindingsAsync()
    {
        var secretsDocument = await _secretBindingsManager.LoadSecretBindingsDocumentAsync();
        return secretsDocument.SecretBindings;
    }

    public IReadOnlyCollection<SecretStoreDescriptor> GetSecretStoreDescriptors() => _secretStoreDescriptors;

    public async Task UpdateSecretAsync(string key, SecretBinding secretBinding, Secret secret)
    {
        if (!String.Equals(key, key.ToSafeName(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The name contains invalid characters.");
        }

        var secretStore = _stores.FirstOrDefault(store => String.Equals(store.Name, secretBinding.Store, StringComparison.OrdinalIgnoreCase));
        if (secretStore is not null)
        {
            await _secretBindingsManager.UpdateSecretBindingAsync(key, secretBinding);

            // This is a noop rather than an exception as updating a readonly store is considered a noop.
            if (!secretStore.IsReadOnly)
            {
                await secretStore.UpdateSecretAsync(key, secret);
            }
        }
        else
        {
            throw new InvalidOperationException($"The specified store '{secretBinding.Store}' was not found.");
        }
    }

    public async Task RemoveSecretAsync(string key, string storeName)
    {
        var secretStore = _stores.FirstOrDefault(store => String.Equals(store.Name, storeName, StringComparison.OrdinalIgnoreCase));
        if (secretStore is not null)
        {
            await _secretBindingsManager.RemoveSecretBindingAsync(key);

            // This is a noop rather than an exception as updating a readonly store is considered a noop.
            if (!secretStore.IsReadOnly)
            {
                await secretStore.RemoveSecretAsync(key);
            }
        }
        else
        {
            throw new InvalidOperationException($"The specified store '{storeName}' was not found.");
        }
    }
}
