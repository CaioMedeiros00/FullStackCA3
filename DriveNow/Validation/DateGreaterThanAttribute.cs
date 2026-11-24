using System.ComponentModel.DataAnnotations;

namespace DriveNow.Validation;

/// <summary>Validates that a date property is later than another date property on the same object.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class DateGreaterThanAttribute(string otherPropertyName) : ValidationAttribute
{
    public string OtherPropertyName { get; } = otherPropertyName;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(OtherPropertyName);
        if (otherProperty is null)
        {
            return new ValidationResult($"Unknown property {OtherPropertyName}.");
        }

        if (value is DateTime thisDate &&
            otherProperty.GetValue(validationContext.ObjectInstance) is DateTime otherDate &&
            thisDate <= otherDate)
        {
            return new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} must be after {OtherPropertyName}.",
                [validationContext.MemberName ?? string.Empty]);
        }

        return ValidationResult.Success;
    }
}
