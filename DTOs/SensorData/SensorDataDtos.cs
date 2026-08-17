using System.ComponentModel.DataAnnotations;

namespace SmartHomeIoT.Api.DTOs.SensorData;

public record SensorDataDto(
    long DataId,
    int DeviceId,
    string SensorType,
    decimal Value,
    string Unit,
    DateTime Timestamp
);

/// <summary>
/// Mirrors the documented MQTT payload (requirement C-04):
/// { "deviceId", "sensorType", "value", "unit", "timestamp" }.
/// Exposed as a REST endpoint too so the API can be used/tested independently of the MQTT broker.
/// </summary>
public class SensorDataCreateDto
{
    [Required]
    public int DeviceId { get; set; }

    /// <summary>"temperature" | "humidity" | "motion" | "light" | "power".</summary>
    [Required, MaxLength(50)]
    public string SensorType { get; set; } = string.Empty;

    [Required]
    public decimal Value { get; set; }

    /// <summary>One of the fixed set: °C, %, W, lux, bool (requirement D-04).</summary>
    [Required, MaxLength(10)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>UTC ISO 8601 timestamp. Defaults to "now" if omitted.</summary>
    public DateTime? Timestamp { get; set; }
}

public static class SensorDataMappingExtensions
{
    public static SensorDataDto ToDto(this Models.SensorData s) =>
        new(s.DataId, s.DeviceId, s.SensorType, s.Value, s.Unit, s.Timestamp);
}
