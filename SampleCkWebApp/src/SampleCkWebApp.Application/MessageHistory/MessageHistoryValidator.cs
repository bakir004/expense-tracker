using ErrorOr;
using SampleCkWebApp.Domain.Errors;
namespace SampleCkWebApp.Application.MessageHistory;

public static class MessageHistoryValidator
{
    public static ErrorOr<Success> ValidateGetMessageHistoryRequest(DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (startTime >= endTime)
        {
            return MessageHistoryErrors.InvalidDateRange;
        }

        if (startTime < DateTimeOffset.MinValue || endTime > DateTimeOffset.MaxValue)
        {
            return MessageHistoryErrors.OutOfRange;
        }

        return new Success();
    }
}