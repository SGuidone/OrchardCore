using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.Fields;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;

namespace OrchardCore.ContentsTransfer.Handlers.Fields;

public class DateTimeFieldImportHandler : StandardFieldImportHandler
{
    public DateTimeFieldImportHandler(IStringLocalizer<DateTimeFieldImportHandler> stringLocalizer)
        : base(stringLocalizer)
    {
    }

    protected override Task SetValueAsync(ContentFieldImportMapContext context, string text)
    {
        if (!string.IsNullOrEmpty(text) && DateTime.TryParse(text.Trim(), out var decimalValue))
        {
            context.ContentPart.Alter<DateTimeField>(context.ContentPartFieldDefinition.Name, (field) =>
            {
                field.Value = decimalValue;
            });
        }

        return Task.CompletedTask;
    }

    protected override Task<object> GetValueAsync(ContentFieldExportMapContext context)
    {
        var field = context.ContentPart.Get<DateTimeField>(context.ContentPartFieldDefinition.Name);

        return Task.FromResult<object>(field?.Value);
    }

    protected override string Description(ImportContentFieldContext context)
        => S["A datetime value for {0}", context.ContentPartFieldDefinition.DisplayName()];


    protected override bool IsRequired(ImportContentFieldContext context)
    {
        var settings = context.ContentPartFieldDefinition.GetSettings<DateFieldSettings>();

        return settings?.Required ?? false;
    }

    protected override string BindingPropertyName
        => nameof(DateTimeField.Value);
}
