using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentsTransfer.Handlers.Fields;

public class TimeFieldImportHandler : StandardFieldImportHandler
{
    protected readonly IStringLocalizer S;

    public TimeFieldImportHandler(IStringLocalizer<TimeFieldImportHandler> stringLocalizer)
    {
        S = stringLocalizer;
    }

    protected override Task SetValueAsync(ContentFieldImportMapContext context, string text)
    {
        if (!string.IsNullOrEmpty(text) && TimeSpan.TryParse(text.Trim(), out var value))
        {
            context.ContentPart.Alter<TimeField>(context.ContentPartFieldDefinition.Name, (field) =>
            {
                field.Value = value;
            });
        }
        return Task.CompletedTask;
    }

    protected override Task<object> GetValueAsync(ContentFieldExportMapContext context)
    {
        var field = context.ContentPart.Get<TimeField>(context.ContentPartFieldDefinition.Name);

        return Task.FromResult<object>(field?.Value);
    }

    protected override string Description(ImportContentFieldContext context)
        => S["A timespan value for {0}", context.ContentPartFieldDefinition.DisplayName()];

    protected override bool IsRequired(ImportContentFieldContext context)
        => context.ContentPartFieldDefinition.GetSettings<TimeFieldSettings>()?.Required ?? false;

    protected override string BindingPropertyName
        => nameof(TimeField.Value);
}
