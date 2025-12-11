namespace SampleCkWebApp.MessageHistory;

public class GetMessageHistoryResponse
{
    public DateTimeOffset StartDate { get; set; }
    
    public DateTimeOffset EndDate { get; set; }
    
    public List<HistoricalMessage> Messages { get; set; } = new();
}