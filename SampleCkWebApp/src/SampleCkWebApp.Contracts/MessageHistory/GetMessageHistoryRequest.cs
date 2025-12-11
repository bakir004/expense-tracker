namespace SampleCkWebApp.MessageHistory;

public class GetMessageHistoryRequest
{
    public DateTimeOffset StartDate { get; set; }
    
    public DateTimeOffset EndDate { get; set; }
}