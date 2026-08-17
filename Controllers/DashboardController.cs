using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeIoT.Api.Data;
using SmartHomeIoT.Api.DTOs.Dashboard;
using SmartHomeIoT.Api.DTOs.EventLog;
using SmartHomeIoT.Api.Models;

namespace SmartHomeIoT.Api.Controllers;

/// <summary>Aggregated data for the dashboard front page (room count, device counts, online/offline).</summary>
[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public DashboardController(SmartHomeDbContext db) => _db = db;

    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var totalRooms = await _db.Rooms.CountAsync();
        var totalDevices = await _db.Devices.CountAsync();
        var onlineDevices = await _db.Devices.CountAsync(d => d.Status == DeviceStatus.Online);
        var offlineDevices = totalDevices - onlineDevices;

        var since = DateTime.UtcNow.AddHours(-24);
        var measurements24h = await _db.SensorData.CountAsync(s => s.Timestamp >= since);

        var recentEvents = await _db.EventLogs
            .AsNoTracking()
            .Include(e => e.Device)
            .OrderByDescending(e => e.Timestamp)
            .Take(10)
            .Select(e => e.ToDto())
            .ToListAsync();

        var summary = new DashboardSummaryDto(
            totalRooms, totalDevices, onlineDevices, offlineDevices, measurements24h, recentEvents);

        return Ok(summary);
    }
}
