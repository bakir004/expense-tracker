namespace SampleCkWebApp.Application.MessageHistory.Data;

public class GetMessageHistoryResult
{
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public List<MessageHistoryRecord> Messages { get; set; }
}