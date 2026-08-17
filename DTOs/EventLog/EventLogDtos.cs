using System.ComponentModel.DataAnnotations;

namespace SmartHomeIoT.Api.DTOs.EventLog;

public record EventLogDto(
    long EventId,
    int? DeviceId,
    string? DeviceName,
    string Event,
    string? Description,
    DateTime Timestamp
);

/// <summary>For manually-logged/administrative events. Most events are written internally by the API itself.</summary>
public class EventLogCreateDto
{
    public int? DeviceId { get; set; }

    [Required, MaxLength(100)]
    public string Event { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public static class EventLogMappingExtensions
{
    public static EventLogDto ToDto(this Models.EventLog e) =>
        new(e.EventId, e.DeviceId, e.Device?.Name, e.Event, e.Description, e.Timestamp);
}
