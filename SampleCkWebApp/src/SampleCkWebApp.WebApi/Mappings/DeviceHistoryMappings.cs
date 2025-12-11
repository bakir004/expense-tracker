using SampleCkWebApp.Application.MessageHistory.Data;
using SampleCkWebApp.MessageHistory;

namespace SampleCkWebApp.WebApi.Mappings;

public static class DeviceHistoryMappings
{
    public static GetMessageHistoryResponse ToResponse(this GetMessageHistoryResult result)
    {
        return new GetMessageHistoryResponse
        {
            StartDate = result.StartDate,
            EndDate = result.EndDate,
            Messages = result.Messages.Select(m => m.ToResponse()).ToList()
        };
    }
    
    public static HistoricalMessage ToResponse(this MessageHistoryRecord record)
    {
        return new HistoricalMessage
        {
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            Timestamp = record.Timestamp,
            DeviceId = record.DeviceId,
        };
    }
}