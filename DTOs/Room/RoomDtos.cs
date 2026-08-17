using System.ComponentModel.DataAnnotations;
using SmartHomeIoT.Api.DTOs.Device;

namespace SmartHomeIoT.Api.DTOs.Room;

/// <summary>Room summary, as shown in the room overview list.</summary>
public record RoomDto(int RoomId, string Name, int DeviceCount);

/// <summary>Room detail including the devices currently assigned to it.</summary>
public record RoomDetailDto(int RoomId, string Name, IReadOnlyList<DeviceDto> Devices);

public class RoomCreateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class RoomUpdateDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
