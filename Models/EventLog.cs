using System.ComponentModel.DataAnnotations;

namespace SmartHomeIoT.Api.Models;

/// <summary>
/// A system event: device registered, device offline/online, invalid measurement, command executed,
/// room created/renamed/deleted, etc. Shown in the dashboard's "Hændelseslog".
///
/// DeviceId is nullable because some events (room CRUD) are not tied to a specific device, and because
/// events about a since-removed device are preserved (DeviceId is set to null on device deletion rather
/// than the event itself being deleted - see README "Design assumptions").
/// </summary>
public class EventLog
{
    public long EventId { get; set; }

    public int? DeviceId { get; set; }
    public Device? Device { get; set; }

    /// <summary>Short machine-friendly event type, e.g. "DeviceRegistered", "DeviceOffline", "InvalidMeasurement".</summary>
    [Required]
    [MaxLength(100)]
    public string Event { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
