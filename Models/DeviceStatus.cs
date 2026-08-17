namespace SmartHomeIoT.Api.Models;

/// <summary>
/// Online/offline status of a paired IoT device, tracked via MQTT heartbeats
/// (heartbeat every &lt;= 60s, offline after 150s of silence - see requirement C-06).
/// </summary>
public enum DeviceStatus
{
    Online = 0,
    Offline = 1
}
