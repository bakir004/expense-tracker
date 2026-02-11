using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ExpenseTrackerAPI.Contracts.Validation;

/// <summary>
/// Validates that a string is a valid date in the specified format.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class DateFormatAttribute : ValidationAttribute
{
    public string Format { get; }

    public DateFormatAttribute(string format = "yyyy-MM-dd")
    {
        Format = format;
        ErrorMessage = $"Invalid date format. Expected {format} (e.g. 2024-12-31).";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
            return ValidationResult.Success; // Let [Required] handle null/empty

        if (value is not string dateString)
            return new ValidationResult(ErrorMessage);

        if (!DateOnly.TryParseExact(dateString.Trim(), Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            return new ValidationResult(ErrorMessage, new[] { validationContext.MemberName! });

        return ValidationResult.Success;
    }
}
