using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;
using OrchardCore.AuditTrail.Models;

namespace OrchardCore.AuditTrail.Services.Models
{
    public class DescribeFor
    {
        private readonly IList<AuditTrailEventDescriptor> _events = new List<AuditTrailEventDescriptor>();

        public DescribeFor(string category, string providerName, LocalizedString name)
        {
            Name = name;
            Category = category;
            ProviderName = providerName;
        }

        public LocalizedString Name { get; private set; }
        public string Category { get; private set; }
        public string ProviderName { get; set; }

        public IEnumerable<AuditTrailEventDescriptor> Events => _events;

        public DescribeFor Event(
            string eventName,
            LocalizedString name,
            LocalizedString description,
            Action<AuditTrailEvent, Dictionary<string, object>> buildAuditTrailEvent,
            bool enableByDefault = false,
            bool isMandatory = false)
        {
            _events.Add(new AuditTrailEventDescriptor
            {
                FullEventName = $"{ProviderName}.{eventName}",
                EventName = eventName,
                LocalizedName = name,
                Description = description,
                BuildAuditTrailEvent = buildAuditTrailEvent,
                IsEnabledByDefault = enableByDefault,
                IsMandatory = isMandatory,
                CategoryDescriptor = new AuditTrailCategoryDescriptor
                {
                    LocalizedName = Name,
                    Category = Category,
                    Events = Events
                }
            });

            return this;
        }
    }
}
