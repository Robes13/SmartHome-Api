using System.ComponentModel.DataAnnotations;
using SmartHomeIoT.Api.Models;

namespace SmartHomeIoT.Api.DTOs.Device;

/// <summary>Device summary, as shown in the device overview list (HK requirements: name, type, status, last comm.).</summary>
public record DeviceDto(
    int DeviceId,
    string Name,
    string Type,
    int RoomId,
    string? RoomName,
    string Status,
    DateTime? LastSeen
);

/// <summary>Full device detail (requirement HK-14: name, id, ip, type, room, registration date).</summary>
public record DeviceDetailDto(
    int DeviceId,
    string Name,
    string Type,
    int RoomId,
    string? RoomName,
    string MacAddress,
    string? IPv4Address,
    string Status,
    DateTime RegistrationDate,
    DateTime? LastSeen
);

/// <summary>
/// Manual/administrative device registration payload. In production the normal path is the
/// WiFi pairing + MQTT-heartbeat flow (see the sequence diagrams) which registers a device
/// automatically; this endpoint lets an operator register or correct a device record directly.
/// </summary>
public class DeviceCreateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public int RoomId { get; set; }

    [Required, MaxLength(17), RegularExpression(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$",
        ErrorMessage = "MacAddress must be in the form AA:BB:CC:DD:EE:FF.")]
    public string MacAddress { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IPv4Address { get; set; }
}

public class DeviceUpdateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    public int RoomId { get; set; }

    [MaxLength(45)]
    public string? IPv4Address { get; set; }
}

/// <summary>Payload for sending a control command to a device (requirement HK-11), e.g. lamp ON/OFF.</summary>
public class DeviceCommandDto
{
    /// <summary>e.g. "ON", "OFF".</summary>
    [Required, MaxLength(50)]
    public string Command { get; set; } = string.Empty;
}

public static class DeviceMappingExtensions
{
    public static DeviceDto ToDto(this Models.Device d) => new(
        d.DeviceId, d.Name, d.Type, d.RoomId, d.Room?.Name, d.Status.ToString(), d.LastSeen);

    public static DeviceDetailDto ToDetailDto(this Models.Device d) => new(
        d.DeviceId, d.Name, d.Type, d.RoomId, d.Room?.Name, d.MacAddress, d.IPv4Address,
        d.Status.ToString(), d.RegistrationDate, d.LastSeen);
}
