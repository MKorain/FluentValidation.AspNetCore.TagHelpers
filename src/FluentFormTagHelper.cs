using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace FluentValidation.AspNetCore.TagHelpers
{
    /// <summary>
    /// Tag Helper that automatically applies FluentValidation rules to form inputs
    /// as jQuery Unobtrusive Validation data-val-* attributes.
    /// </summary>
    [HtmlTargetElement("form", Attributes = FluentModelAttributeName)]
    public class FluentFormTagHelper : TagHelper
    {
        private const string FluentModelAttributeName = "asp-fluent-model";
        
        // Cache for validators and descriptors to optimize performance
        private static readonly ConcurrentDictionary<Type, object> _validatorCache = new();
        private static readonly ConcurrentDictionary<Type, IValidatorDescriptor> _descriptorCache = new();
        
        private readonly IServiceProvider _serviceProvider;
        
        [HtmlAttributeName(FluentModelAttributeName)]
        public ModelExpression FluentModel { get; set; }
        
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public FluentFormTagHelper(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public override int Order => -1000; // Execute before other tag helpers

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (FluentModel?.ModelExplorer?.ModelType == null)
            {
                return;
            }

            var modelType = FluentModel.ModelExplorer.ModelType;
            
            // Get or create validator
            var validator = GetOrCreateValidator(modelType);
            if (validator == null)
            {
                return;
            }

            // Get or create descriptor
            var descriptor = GetOrCreateDescriptor(validator);
            if (descriptor == null)
            {
                return;
            }

            // Store validation rules in ViewContext for use by input elements
            var validationRules = BuildValidationRulesMap(descriptor, modelType);
            ViewContext.ViewData[$"__FluentValidationRules_{modelType.FullName}"] = validationRules;
            
            // Add a custom attribute to mark this form as fluent-validated
            output.Attributes.Add("data-fluent-validation", "true");
        }

        private object GetOrCreateValidator(Type modelType)
        {
            return _validatorCache.GetOrAdd(modelType, type =>
            {
                var validatorType = typeof(IValidator<>).MakeGenericType(type);
                return _serviceProvider.GetService(validatorType);
            });
        }

        private IValidatorDescriptor GetOrCreateDescriptor(object validator)
        {
            if (validator == null) return null;
            
            var validatorType = validator.GetType();
            return _descriptorCache.GetOrAdd(validatorType, _ =>
            {
                var method = validatorType.GetMethod("CreateDescriptor");
                return method?.Invoke(validator, null) as IValidatorDescriptor;
            });
        }

        private Dictionary<string, List<ValidationRule>> BuildValidationRulesMap(
            IValidatorDescriptor descriptor, 
            Type modelType,
            string prefix = "")
        {
            var rulesMap = new Dictionary<string, List<ValidationRule>>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var member in descriptor.GetMembersWithValidators())
            {
                var propertyName = string.IsNullOrEmpty(prefix) 
                    ? member.Key 
                    : $"{prefix}.{member.Key}";
                
                var rules = new List<ValidationRule>();
                
                foreach (var validatorWrapper in member.Value)
                {
                    var propertyValidator = validatorWrapper.Validator;
                    var validationRule = CreateValidationRule(propertyValidator, validatorWrapper);
                    
                    if (validationRule != null)
                    {
                        rules.Add(validationRule);
                    }
                }
                
                if (rules.Any())
                {
                    rulesMap[propertyName] = rules;
                }
                
                // Handle nested properties
                var propertyInfo = modelType.GetProperty(member.Key);
                if (propertyInfo != null && IsComplexType(propertyInfo.PropertyType))
                {
                    var nestedValidator = GetOrCreateValidator(propertyInfo.PropertyType);
                    if (nestedValidator != null)
                    {
                        var nestedDescriptor = GetOrCreateDescriptor(nestedValidator);
                        if (nestedDescriptor != null)
                        {
                            var nestedRules = BuildValidationRulesMap(
                                nestedDescriptor, 
                                propertyInfo.PropertyType, 
                                propertyName);
                             
                            foreach (var kvp in nestedRules)
                            {
                                rulesMap[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
            }
            
            return rulesMap;
        }

        private ValidationRule CreateValidationRule(IPropertyValidator propertyValidator, IValidationRule rule)
        {
            var errorMessage = GetErrorMessage(propertyValidator, rule);
            
            return propertyValidator switch
            {
                INotNullValidator => new ValidationRule
                {
                    ValidationType = "required",
                    ErrorMessage = errorMessage ?? "This field is required."
                },
                
                INotEmptyValidator => new ValidationRule
                {
                    ValidationType = "required",
                    ErrorMessage = errorMessage ?? "This field is required."
                },
                
                IEmailValidator => new ValidationRule
                {
                    ValidationType = "email",
                    ErrorMessage = errorMessage ?? "Please enter a valid email address."
                },
                
                ILengthValidator lengthValidator => new ValidationRule
                {
                    ValidationType = "length",
                    ErrorMessage = errorMessage ?? $"Must be between {lengthValidator.Min} and {lengthValidator.Max} characters.",
                    Parameters = new Dictionary<string, object>
                    {
                        ["min"] = lengthValidator.Min,
                        ["max"] = lengthValidator.Max
                    }
                },
                
                IMaximumLengthValidator maxLengthValidator => new ValidationRule
                {
                    ValidationType = "maxlength",
                    ErrorMessage = errorMessage ?? $"Must not exceed {maxLengthValidator.Max} characters.",
                    Parameters = new Dictionary<string, object>
                    {
                        ["max"] = maxLengthValidator.Max
                    }
                },
                
                IMinimumLengthValidator minLengthValidator => new ValidationRule
                {
                    ValidationType = "minlength",
                    ErrorMessage = errorMessage ?? $"Must be at least {minLengthValidator.Min} characters.",
                    Parameters = new Dictionary<string, object>
                    {
                        ["min"] = minLengthValidator.Min
                    }
                },
                
                IBetweenValidator betweenValidator => CreateRangeRule(betweenValidator, errorMessage),
                
                IComparisonValidator comparisonValidator => CreateComparisonRule(comparisonValidator, errorMessage),
                
                IRegularExpressionValidator regexValidator => new ValidationRule
                {
                    ValidationType = "regex",
                    ErrorMessage = errorMessage ?? "Invalid format.",
                    Parameters = new Dictionary<string, object>
                    {
                        ["pattern"] = regexValidator.Expression
                    }
                },
                
                ICreditCardValidator => new ValidationRule
                {
                    ValidationType = "creditcard",
                    ErrorMessage = errorMessage ?? "Please enter a valid credit card number."
                },
                
                _ when IsCustomValidator(propertyValidator) => new ValidationRule
                {
                    ValidationType = "custom",
                    ErrorMessage = errorMessage ?? "Invalid value."
                },
                
                _ => null
            };
        }

        private ValidationRule CreateRangeRule(IBetweenValidator betweenValidator, string errorMessage)
        {
            var from = GetPropertyValue(betweenValidator, "From");
            var to = GetPropertyValue(betweenValidator, "To");
            
            return new ValidationRule
            {
                ValidationType = "range",
                ErrorMessage = errorMessage ?? $"Must be between {from} and {to}.",
                Parameters = new Dictionary<string, object>
                {
                    ["min"] = from,
                    ["max"] = to
                }
            };
        }

        private ValidationRule CreateComparisonRule(IComparisonValidator comparisonValidator, string errorMessage)
        {
            var valueToCompare = GetPropertyValue(comparisonValidator, "ValueToCompare");
            var comparison = GetPropertyValue(comparisonValidator, "Comparison");
            
            return comparison?.ToString() switch
            {
                "GreaterThan" or "GreaterThanOrEqual" => new ValidationRule
                {
                    ValidationType = "min",
                    ErrorMessage = errorMessage ?? $"Must be greater than {valueToCompare}.",
                    Parameters = new Dictionary<string, object>
                    {
                        ["min"] = valueToCompare
                    }
                },
                
                "LessThan" or "LessThanOrEqual" => new ValidationRule
                {
                    ValidationType = "max",
                    ErrorMessage = errorMessage ?? $"Must be less than {valueToCompare}.",
                    Parameters = new Dictionary<string, object>
                    {
                        ["max"] = valueToCompare
                    }
                },
                
                _ => null
            };
        }

        private string GetErrorMessage(IPropertyValidator validator, IValidationRule rule)
        {
            try
            {
                var errorMessageSource = rule.GetType()
                    .GetProperty("ErrorMessage", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(rule);

                if (errorMessageSource != null)
                {
                    if (errorMessageSource is string errorString)
                    {
                        return errorString;
                    }
                }

                var messageProp = validator.GetType()
                    .GetProperty("ErrorMessageSource", BindingFlags.Public | BindingFlags.Instance);
                
                if (messageProp != null)
                {
                    var value = messageProp.GetValue(validator);
                    if (value is string msg)
                    {
                        return msg;
                    }
                }
            }
            catch
            {
            }
            
            return null;
        }

        private object GetPropertyValue(object obj, string propertyName)
        {
            try
            {
                var property = obj.GetType().GetProperty(propertyName, 
                    BindingFlags.Public | BindingFlags.Instance);
                return property?.GetValue(obj);
            }
            catch
            {
                return null;
            }
        }

        private bool IsCustomValidator(IPropertyValidator validator)
        {
            var type = validator.GetType();
            return type.Name.Contains("Custom") || 
                   type.Name.Contains("Predicate") ||
                   type.GetInterfaces().Any(i => i.Name.Contains("ICustomValidator"));
        }

        private bool IsComplexType(Type type)
        {
            if (type.IsPrimitive || 
                type == typeof(string) || 
                type == typeof(DateTime) || 
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type == typeof(decimal) ||
                type.IsEnum)
            {
                return false;
            }
            
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return false;
            }
            
            if (type.IsArray || 
                typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }
            
            return type.IsClass || type.IsValueType;
        }
    }

    public class ValidationRule
    {
        public string ValidationType { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    [HtmlTargetElement("input", Attributes = ForAttributeName)]
    [HtmlTargetElement("select", Attributes = ForAttributeName)]
    [HtmlTargetElement("textarea", Attributes = ForAttributeName)]
    public class FluentValidationInputTagHelper : TagHelper
    {
        private const string ForAttributeName = "asp-for";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override int Order => 1000;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (For?.ModelExplorer == null)
            {
                return;
            }

            var modelType = ViewContext.ViewData.ModelMetadata.ModelType;
            var rulesKey = $"__FluentValidationRules_{modelType.FullName}";
            
            if (!ViewContext.ViewData.ContainsKey(rulesKey))
            {
                return;
            }

            var validationRules = ViewContext.ViewData[rulesKey] 
                as Dictionary<string, List<ValidationRule>>;
            
            if (validationRules == null)
            {
                return;
            }

            var propertyName = For.Name;
            
            if (validationRules.TryGetValue(propertyName, out var rules))
            {
                output.Attributes.SetAttribute("data-val", "true");
                
                foreach (var rule in rules)
                {
                    ApplyValidationRule(output, rule);
                }
            }
        }

        private void ApplyValidationRule(TagHelperOutput output, ValidationRule rule)
        {
            var attributeName = $"data-val-{rule.ValidationType}";
            output.Attributes.SetAttribute(attributeName, rule.ErrorMessage);
            
            if (rule.Parameters != null)
            {
                foreach (var param in rule.Parameters)
                {
                    var paramAttributeName = $"{attributeName}-{param.Key}";
                    output.Attributes.SetAttribute(paramAttributeName, param.Value?.ToString() ?? "");
                }
            }
        }
    }
}