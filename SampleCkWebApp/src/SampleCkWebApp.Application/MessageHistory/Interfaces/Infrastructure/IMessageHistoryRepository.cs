using ErrorOr;
using SampleCkWebApp.Application.MessageHistory.Data;

namespace SampleCkWebApp.Application.MessageHistory.Interfaces.Infrastructure;

public interface IMessageHistoryRepository
{
    Task<ErrorOr<List<MessageHistoryRecord>>> GetMessageHistoryAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken token);
}