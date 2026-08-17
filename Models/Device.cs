using System.ComponentModel.DataAnnotations;

namespace SmartHomeIoT.Api.Models;

/// <summary>
/// A registered/paired IoT device (Arduino Uno R4 WiFi / Arduino Opta Pro).
/// The device's physical identity is its MAC address, never its IPv4 address -
/// IPv4 is assigned by DHCP and can change between reconnects.
/// </summary>
public class Device
{
    public int DeviceId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>e.g. "temperature", "humidity", "motion", "light", "power".</summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    /// <summary>Permanent physical identity of the device. Unique. Never used interchangeably with IPv4.</summary>
    [Required]
    [MaxLength(17)] // AA:BB:CC:DD:EE:FF
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>Current network address, assigned by DHCP. Mutable - overwritten on every heartbeat.</summary>
    [MaxLength(45)] // supports IPv4 and, if ever needed, IPv6
    public string? IPv4Address { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Online;

    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp of the last heartbeat/measurement received from this device (UTC).</summary>
    public DateTime? LastSeen { get; set; }

    public ICollection<SensorData> SensorReadings { get; set; } = new List<SensorData>();
    public ICollection<EventLog> Events { get; set; } = new List<EventLog>();
}
