using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeIoT.Api.Models;

/// <summary>
/// A single validated measurement received from a device.
/// Rejected (out-of-range) measurements are never stored here - see EventLog instead (requirement D-05).
/// </summary>
public class SensorData
{
    public long DataId { get; set; }

    public int DeviceId { get; set; }
    public Device? Device { get; set; }

    /// <summary>"temperature" | "humidity" | "motion" | "light" | "power".</summary>
    [Required]
    [MaxLength(50)]
    public string SensorType { get; set; } = string.Empty;

    /// <summary>Stored as DECIMAL(8,2) per requirement D-03.</summary>
    [Column(TypeName = "decimal(8,2)")]
    public decimal Value { get; set; }

    /// <summary>Restricted to the fixed set from requirement D-04: °C, %, W, lux, bool.</summary>
    [Required]
    [MaxLength(10)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>Stored in UTC, ISO 8601 on the wire (requirement D-02).</summary>
    public DateTime Timestamp { get; set; }
}
