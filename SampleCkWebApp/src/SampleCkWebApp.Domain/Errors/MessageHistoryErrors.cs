using ErrorOr;

namespace SampleCkWebApp.Domain.Errors;

public static class MessageHistoryErrors
{
    public static Error InvalidDateRange =>
        Error.Validation($"{nameof(MessageHistoryErrors)}.{nameof(InvalidDateRange)}", "The date range is invalid. Please ensure the start date is before the end date.");
    
    public static Error OutOfRange =>
        Error.Validation($"{nameof(MessageHistoryErrors)}.{nameof(OutOfRange)}", "The date range is out of the valid range. Please check the start and end dates.");
}