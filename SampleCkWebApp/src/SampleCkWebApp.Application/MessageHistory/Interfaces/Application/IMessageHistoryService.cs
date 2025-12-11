using ErrorOr;
using SampleCkWebApp.Application.MessageHistory.Data;

namespace SampleCkWebApp.Application.MessageHistory.Interfaces.Application;

public interface IMessageHistoryService
{
    Task<ErrorOr<GetMessageHistoryResult>> GetMessageHistoryAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken token);
}