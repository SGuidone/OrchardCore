using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using OrchardCore.Documents;
using OrchardCore.Entities;

namespace OrchardCore.Settings
{
    // When updating class also update SiteSettingsDeploymentSource and SettingsStep.
    public class SiteSettings : DocumentEntity, ICacheableEntity, ISite
    {
        private readonly Dictionary<string, object> _cache = new();

        private JObject _properties = new();

        public string BaseUrl { get; set; }
        public string Calendar { get; set; }
        public int MaxPagedCount { get; set; }
        public int MaxPageSize { get; set; }
        public int PageSize { get; set; }
        public string TimeZoneId { get; set; }
        public ResourceDebugMode ResourceDebugMode { get; set; }
        public string SiteName { get; set; }
        public string SiteSalt { get; set; }
        public string PageTitleFormat { get; set; }
        public string SuperUser { get; set; }
        public bool UseCdn { get; set; }
        public string CdnBaseUrl { get; set; }
        public RouteValueDictionary HomeRoute { get; set; } = new RouteValueDictionary();
        public bool AppendVersion { get; set; } = true;
        public CacheMode CacheMode { get; set; }

        public new JObject Properties
        {
            get => _properties;
            set
            {
                _properties = value ?? new JObject();
                _cache.Clear();
            }
        }

        public void Remove(string key)
        {
            AssertNotNull(key);

            _cache.Remove(key, out _);
        }

        public object Get(string key)
            => _cache.TryGetValue(key, out var value) ? value : default;

        public void Set(string key, object value)
        {
            AssertNotNull(key);

            _cache[key] = value;
        }

        private static void AssertNotNull(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException($"{nameof(key)} cannot be null or empty.");
            }
        }
    }
}
