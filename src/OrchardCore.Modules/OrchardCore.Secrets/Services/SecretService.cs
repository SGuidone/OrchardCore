using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OrchardCore.Mvc.Utilities;
using OrchardCore.Secrets.Models;
using OrchardCore.Secrets.Options;

namespace OrchardCore.Secrets.Services;

public class SecretService : ISecretService
{
    private readonly SecretBindingsManager _bindingsManager;
    private readonly IReadOnlyCollection<SecretStoreInfo> _storeInfos;
    private readonly IEnumerable<ISecretStore> _stores;

    private readonly Dictionary<string, SecretActivator> _activators = new();

    public SecretService(SecretBindingsManager bindingsManager, IEnumerable<ISecretStore> stores, IOptions<SecretOptions> options)
    {
        _bindingsManager = bindingsManager;

        _storeInfos = stores.Select(store => new SecretStoreInfo
        {
            Name = store.Name,
            IsReadOnly = store.IsReadOnly,
            DisplayName = store.DisplayName,
        })
            .ToArray();

        _stores = stores;

        foreach (var type in options.Value.SecretTypes)
        {
            var activatorType = typeof(SecretActivator<>).MakeGenericType(type);
            var activator = (SecretActivator)Activator.CreateInstance(activatorType);
            _activators[type.Name] = activator;
        }
    }

    public SecretBase CreateSecret(string typeName)
    {
        if (!_activators.TryGetValue(typeName, out var factory) || !typeof(SecretBase).IsAssignableFrom(factory.Type))
        {
            throw new ArgumentException($"The type should be configured and should implement '{nameof(SecretBase)}'.", nameof(typeName));
        }

        return factory.Create();
    }

    public async Task<SecretBase> GetSecretAsync(SecretBinding binding)
    {
        if (!_activators.TryGetValue(binding.Type, out var factory) ||
            !typeof(SecretBase).IsAssignableFrom(factory.Type))
        {
            return null;
        }

        var secretStore = _stores.FirstOrDefault(store => String.Equals(store.Name, binding.Store, StringComparison.OrdinalIgnoreCase));
        if (secretStore is null)
        {
            return null;
        }

        var secret = (await secretStore.GetSecretAsync(binding.Name, factory.Type)) ?? factory.Create();

        secret.Name = binding.Name;

        return secret;
    }

    public async Task<IDictionary<string, SecretBinding>> GetSecretBindingsAsync()
    {
        var secretsDocument = await _bindingsManager.GetSecretBindingsDocumentAsync();
        return secretsDocument.SecretBindings;
    }

    public async Task<IDictionary<string, SecretBinding>> LoadSecretBindingsAsync()
    {
        var secretsDocument = await _bindingsManager.LoadSecretBindingsDocumentAsync();
        return secretsDocument.SecretBindings;
    }

    public IReadOnlyCollection<SecretStoreInfo> GetSecretStoreInfos() => _storeInfos;

    public async Task UpdateSecretAsync(SecretBinding binding, SecretBase secret)
    {
        if (!String.Equals(binding.Name, binding.Name.ToSafeName(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The name contains invalid characters.");
        }

        secret.Name = binding.Name;

        var secretStore = _stores.FirstOrDefault(store => String.Equals(store.Name, binding.Store, StringComparison.OrdinalIgnoreCase));
        if (secretStore is not null)
        {
            await _bindingsManager.UpdateSecretBindingAsync(binding.Name, binding);

            // This is a noop rather than an exception as updating a readonly store is considered a noop.
            if (!secretStore.IsReadOnly)
            {
                await secretStore.UpdateSecretAsync(binding.Name, secret);
            }
        }
        else
        {
            throw new InvalidOperationException($"The specified store '{binding.Store}' was not found.");
        }
    }

    public async Task RemoveSecretAsync(SecretBinding binding)
    {
        var store = _stores.FirstOrDefault(store => String.Equals(store.Name, binding.Store, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The specified store '{binding.Store}' was not found.");

        await _bindingsManager.RemoveSecretBindingAsync(binding.Name);

        // This is a noop rather than an exception as updating a readonly store is considered a noop.
        if (!store.IsReadOnly)
        {
            await store.RemoveSecretAsync(binding.Name);
        }
    }
}
