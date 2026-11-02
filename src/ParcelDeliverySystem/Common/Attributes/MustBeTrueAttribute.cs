using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ParcelDeliverySystem.Common.Attributes;

/// <summary>
/// Validation attribute ensuring that a boolean property is true (e.g. Terms & Conditions acceptance checkbox).
/// Generates both server-side validation and client-side unobtrusive validation attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    public MustBeTrueAttribute() : base("You must accept the terms and conditions to proceed.")
    {
    }

    public MustBeTrueAttribute(string errorMessage) : base(errorMessage)
    {
    }

    public override bool IsValid(object? value)
    {
        return value is bool b && b;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        MergeAttribute(context.Attributes, "data-val", "true");
        MergeAttribute(context.Attributes, "data-val-mustbetrue", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
    }

    private static void MergeAttribute(IDictionary<string, string> attributes, string key, string value)
    {
        if (!attributes.ContainsKey(key))
        {
            attributes.Add(key, value);
        }
    }
}
