using System;
using System.Threading.Tasks;
using GraphQL.Validation;
using GraphQLParser.AST;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrchardCore.Apis.GraphQL.ValidationRules
{
    public class MaxNumberOfResultsValidationRule : IValidationRule
    {
        private readonly int _maxNumberOfResults;
        private readonly MaxNumberOfResultsValidationMode _maxNumberOfResultsValidationMode;
        private readonly IStringLocalizer<MaxNumberOfResultsValidationRule> _localizer;
        private readonly ILogger<MaxNumberOfResultsValidationRule> _logger;

        public MaxNumberOfResultsValidationRule(IOptions<GraphQLSettings> options, IStringLocalizer<MaxNumberOfResultsValidationRule> localizer, ILogger<MaxNumberOfResultsValidationRule> logger)
        {
            var settings = options.Value;
            _maxNumberOfResults = settings.MaxNumberOfResults;
            _maxNumberOfResultsValidationMode = settings.MaxNumberOfResultsValidationMode;
            _localizer = localizer;
            _logger = logger;
        }

        public ValueTask<INodeVisitor> ValidateAsync(ValidationContext validationContext)
        {
            return ValueTask.FromResult((INodeVisitor)new NodeVisitors(
            new MatchingNodeVisitor<GraphQLArgument>((arg, visitorContext) =>
            {
                if ((arg.Name == "first" || arg.Name == "last") && arg.Value != null)
                {
                    var context = (GraphQLUserContext)validationContext.UserContext;

                    int? value = null;

                    if (arg.Value is GraphQLIntValue)
                    {
                        value = Int32.Parse((arg.Value as GraphQLIntValue).Value);
                    }
                    else
                    {
                        if (validationContext.Variables.TryGetValue(arg.Value.ToString(), out var input))
                        {
                            value = (int?)input;
                        }
                    }

                    if (value.HasValue && value > _maxNumberOfResults)
                    {
                        var errorMessage = _localizer["'{0}' exceeds the maximum number of results for '{1}' ({2})", value.Value, arg.Name, _maxNumberOfResults];

                        if (_maxNumberOfResultsValidationMode == MaxNumberOfResultsValidationMode.Enabled)
                        {
                            validationContext.ReportError(new ValidationError(
                                validationContext.Document.Source,
                                "ArgumentInputError",
                                errorMessage,
                                arg));
                        }
                        else
                        {
                            _logger.LogInformation(errorMessage);

                            arg = new GraphQLArgument
                            {
                                Name = arg.Name,
                                Value = new GraphQLIntValue(_maxNumberOfResults)
                            }; // if disabled mode we just log info and override the arg to be maxvalue
                        }
                    }
                }
            })));
        }
    }
}
