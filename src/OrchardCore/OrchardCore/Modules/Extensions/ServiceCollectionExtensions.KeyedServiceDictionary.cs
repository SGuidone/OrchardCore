using System.Collections.Generic;
using System.Collections.ObjectModel;
using OrchardCore;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ServiceCollectionExtensions
{
    private sealed class KeyedServiceDictionary<TKey, TService>(IKeyedServiceResolver keyedServiceResolver)
        : ReadOnlyDictionary<TKey, TService>(Create(keyedServiceResolver))
        , IKeyedServiceDictionary<TKey, TService>
        where TKey : notnull
        where TService : notnull
    {
        private static Dictionary<TKey, TService> Create(IKeyedServiceResolver keyedServiceResolver)
        {
            var dictionary = keyedServiceResolver.GetServices<TKey, TService>();

            var collection = new Dictionary<TKey, TService>(capacity: dictionary.Count);

            foreach (var entry in dictionary)
            {
                collection[entry.Key] = entry.Value;
            }

            return collection;
        }
    }
}
