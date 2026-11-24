using System.ComponentModel.DataAnnotations;

namespace DriveNow.Validation;

/// <summary>Validates that a date is today or later.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotInPastAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date && date.Date < DateTime.Today)
        {
            return new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} cannot be in the past.",
                [validationContext.MemberName ?? string.Empty]);
        }

        return ValidationResult.Success;
    }
}
