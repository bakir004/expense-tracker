using ErrorOr;
using SampleCkWebApp.Application.MessageHistory.Data;
using SampleCkWebApp.Application.MessageHistory.Interfaces.Application;
using SampleCkWebApp.Application.MessageHistory.Interfaces.Infrastructure;

namespace SampleCkWebApp.Application.MessageHistory;

public class MessageHistoryService : IMessageHistoryService
{
    private readonly IMessageHistoryRepository _messageHistoryRepository;

    public MessageHistoryService(IMessageHistoryRepository messageHistoryRepository)
    {
        _messageHistoryRepository = messageHistoryRepository;
    }
    
    public async Task<ErrorOr<GetMessageHistoryResult>> GetMessageHistoryAsync(DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken token)
    {
        var validationResult = MessageHistoryValidator.ValidateGetMessageHistoryRequest(startTime, endTime);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }
        
        var getMessageHistoryResult = await _messageHistoryRepository.GetMessageHistoryAsync(startTime, endTime, token);
        if (getMessageHistoryResult.IsError)
        {
            return getMessageHistoryResult.Errors;
        }
        
        // Perform any additional processing on the retrieved messages if necessary.
        
        return new GetMessageHistoryResult
        {
            Messages = getMessageHistoryResult.Value
        };
    }
}