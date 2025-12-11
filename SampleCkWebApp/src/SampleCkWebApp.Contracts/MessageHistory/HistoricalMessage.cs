namespace SampleCkWebApp.MessageHistory;

public class HistoricalMessage
{
    public long Latitude { get; set; }

    public long Longitude { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public string DeviceId { get; set; } = string.Empty;
}