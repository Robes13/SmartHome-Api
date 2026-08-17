using System.ComponentModel.DataAnnotations;

namespace SmartHomeIoT.Api.Models;

/// <summary>
/// A physical room in the home (e.g. "Stue", "Køkken"). Devices are always assigned to exactly one room.
/// </summary>
public class Room
{
    public int RoomId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Navigation property - devices currently assigned to this room.</summary>
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
