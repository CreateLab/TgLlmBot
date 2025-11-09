using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TgLlmBot.Extensions.Configuration;

internal static class TypedConfigurationExtensions
{
    [SuppressMessage("Style", "IDE0063:Use simple \'using\' statement")]
    [SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
    public static TConfiguration GetTypedConfigurationFromOptions<TOptions, TConfiguration>(
        this IConfiguration configuration,
        Func<TOptions, TConfiguration> convert,
        Action<TOptions, IConfiguration>? optionsPostProcessor = null) where TOptions : class, new()
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(convert);
        var services = new ServiceCollection();
        _ = services.AddOptions<TOptions>()
            .Bind(configuration)
            .ValidateDataAnnotationsRecursive();
        using (var provider = services.BuildServiceProvider())
        using (var scope = provider.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<TOptions>>().Value;
            optionsPostProcessor?.Invoke(options, configuration);
            var result = convert(options);
            return result;
        }
    }

    private static OptionsBuilder<TOptions> ValidateDataAnnotationsRecursive<TOptions>(
        this OptionsBuilder<TOptions> optionsBuilder)
        where TOptions : class
    {
        optionsBuilder.Services.AddSingleton(
            (IValidateOptions<TOptions>) new RecursiveDataAnnotationValidateOptions<TOptions>(optionsBuilder.Name));
        return optionsBuilder;
    }

    private sealed class RecursiveDataAnnotationValidateOptions<TOptions>(string? name) : IValidateOptions<TOptions>
        where TOptions : class
    {
        private string? Name { get; } = name;

        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (Name is not null && name != Name)
            {
                return ValidateOptionsResult.Skip;
            }

            var recursiveValidationResult = RecursiveDataAnnotationsValidator.Validate(options);
            return recursiveValidationResult.Succeeded
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(recursiveValidationResult.FailureMessage);
        }
    }

    private static class RecursiveDataAnnotationsValidator
    {
        public static RecursiveValidationResult Validate<TObject>(TObject objectToValidate)
        {
            var validationResults = new List<ValidationResult>();
            var validatedObjects = new HashSet<object>();
            if (TryValidateObjectRecursive(
                    objectToValidate,
                    validationResults,
                    validatedObjects))
            {
                return RecursiveValidationResult.Success();
            }

            var errorMessage = string.Join(
                Environment.NewLine,
                validationResults.Select(r =>
                    "DataAnnotation validation failed for members "
                    + string.Join(", ", r.MemberNames)
                    + " with the error '"
                    + r.ErrorMessage
                    + "'."));
            return RecursiveValidationResult.Failure(errorMessage);
        }

        private static bool TryValidateObjectRecursive<T>(
            T objectToValidate,
            List<ValidationResult> results,
            HashSet<object> validatedObjects)
        {
            var boxedObject = objectToValidate as object;
            ArgumentNullException.ThrowIfNull(boxedObject, nameof(objectToValidate));

            if (!validatedObjects.Add(boxedObject))
            {
                return true;
            }

            var result = Validator.TryValidateObject(
                boxedObject,
                new(boxedObject, null, null),
                results,
                true);

            var properties = boxedObject
                .GetType()
                .GetProperties()
                .Where(prop => prop.CanRead && prop.GetIndexParameters().Length == 0)
                .ToList();

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
                {
                    continue;
                }

                var propertyName = property.Name;
                var value = GetPropertyValue(boxedObject, property.Name);
                switch (value)
                {
                    case null:
                        continue;
                    case IEnumerable asEnumerable:
                        {
                            var childIndex = 0;
                            foreach (var enumObj in asEnumerable)
                            {
                                if (enumObj is not null)
                                {
                                    var nestedResults = new List<ValidationResult>();
                                    if (!TryValidateObjectRecursive(
                                            enumObj,
                                            nestedResults,
                                            validatedObjects))
                                    {
                                        result = false;
                                        foreach (var validationResult in nestedResults)
                                        {
                                            var resultIndex = childIndex;
                                            results.Add(new(
                                                validationResult.ErrorMessage,
                                                validationResult.MemberNames.Select(x => $"{propertyName}[{resultIndex:D}].{x}")));
                                        }
                                    }
                                }

                                childIndex++;
                            }

                            break;
                        }

                    default:
                        {
                            var nestedResults = new List<ValidationResult>();
                            if (!TryValidateObjectRecursive(value, nestedResults, validatedObjects))
                            {
                                result = false;
                                foreach (var validationResult in nestedResults)
                                {
                                    results.Add(new(
                                        validationResult.ErrorMessage,
                                        validationResult.MemberNames.Select(x => $"{propertyName}.{x}")));
                                }
                            }

                            break;
                        }
                }
            }

            return result;
        }

        private static object? GetPropertyValue(object obj, string propertyName)
        {
            object? objValue = null;
            var propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo is not null)
            {
                objValue = propertyInfo.GetValue(obj, null);
            }

            return objValue;
        }
    }

    private sealed class RecursiveValidationResult
    {
        private RecursiveValidationResult(bool succeeded, string? failureMessage)
        {
            Succeeded = succeeded;
            FailureMessage = failureMessage;
        }

        [MemberNotNullWhen(false, nameof(FailureMessage))]
        public bool Succeeded { get; }

        public string? FailureMessage { get; }

        public static RecursiveValidationResult Success()
        {
            return new(true, null);
        }

        public static RecursiveValidationResult Failure(string? failureMessage = null)
        {
            return new(false, failureMessage);
        }
    }
}
