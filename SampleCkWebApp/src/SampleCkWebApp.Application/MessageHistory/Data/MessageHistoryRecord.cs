namespace SampleCkWebApp.Application.MessageHistory.Data;

public class MessageHistoryRecord
{
    public long Id { get; set; }
    
    public long Latitude { get; set; }
    
    public long Longitude { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public string DeviceId { get; set; } = string.Empty;
}