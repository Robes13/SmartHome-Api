using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeIoT.Api.Data;
using SmartHomeIoT.Api.DTOs.EventLog;
using SmartHomeIoT.Api.Models;

namespace SmartHomeIoT.Api.Controllers;

/// <summary>System event log, shown on the dashboard's "Hændelseslog" page.</summary>
[ApiController]
[Route("api/v1/eventlog")]
[Produces("application/json")]
public class EventLogController : ControllerBase
{
    private readonly SmartHomeDbContext _db;

    public EventLogController(SmartHomeDbContext db) => _db = db;

    /// <summary>List events, optionally filtered by device, event type, or time range.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventLogDto>>> GetAll(
        [FromQuery] int? deviceId,
        [FromQuery] string? eventType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int take = 200)
    {
        var query = _db.EventLogs.AsNoTracking().Include(e => e.Device).AsQueryable();

        if (deviceId is not null) query = query.Where(e => e.DeviceId == deviceId);
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.Event == eventType);
        if (from is not null) query = query.Where(e => e.Timestamp >= from);
        if (to is not null) query = query.Where(e => e.Timestamp <= to);

        var events = await query
            .OrderByDescending(e => e.Timestamp)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(e => e.ToDto())
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Get a single event log entry.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(EventLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventLogDto>> GetById(long id)
    {
        var entry = await _db.EventLogs.AsNoTracking().Include(e => e.Device).FirstOrDefaultAsync(e => e.EventId == id);
        if (entry is null)
            return NotFound(new { statusCode = 404, message = $"EventLog {id} was not found." });

        return Ok(entry.ToDto());
    }

    /// <summary>Manually write an event (administrative use - most events are written internally by the API).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventLogDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventLogDto>> Create(EventLogCreateDto dto)
    {
        if (dto.DeviceId is not null)
        {
            var exists = await _db.Devices.AnyAsync(d => d.DeviceId == dto.DeviceId);
            if (!exists)
                return NotFound(new { statusCode = 404, message = $"Device {dto.DeviceId} was not found." });
        }

        var entry = new EventLog
        {
            DeviceId = dto.DeviceId,
            Event = dto.Event,
            Description = dto.Description,
            Timestamp = DateTime.UtcNow
        };

        _db.EventLogs.Add(entry);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entry.EventId }, entry.ToDto());
    }
}
