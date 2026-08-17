using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHomeIoT.Api.Data;
using SmartHomeIoT.Api.DTOs.SensorData;
using SmartHomeIoT.Api.Models;
using SmartHomeIoT.Api.Services;

namespace SmartHomeIoT.Api.Controllers;

/// <summary>
/// Sensor measurements. This mirrors what the backend's MQTT listener does when a message
/// arrives on home/{roomId}/{deviceId}/{sensorType}: parse, validate against the documented
/// range table, and either store it (HK-13) or reject it into EventLog (D-05).
/// </summary>
[ApiController]
[Route("api/v1/sensordata")]
[Produces("application/json")]
public class SensorDataController : ControllerBase
{
    private readonly SmartHomeDbContext _db;
    private readonly ISensorValidationService _validator;

    public SensorDataController(SmartHomeDbContext db, ISensorValidationService validator)
    {
        _db = db;
        _validator = validator;
    }

    /// <summary>Query stored measurements, optionally filtered by device, sensor type and time range.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SensorDataDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SensorDataDto>>> GetAll(
        [FromQuery] int? deviceId,
        [FromQuery] string? sensorType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int take = 200)
    {
        var query = _db.SensorData.AsNoTracking().AsQueryable();

        if (deviceId is not null) query = query.Where(s => s.DeviceId == deviceId);
        if (!string.IsNullOrWhiteSpace(sensorType)) query = query.Where(s => s.SensorType == sensorType);
        if (from is not null) query = query.Where(s => s.Timestamp >= from);
        if (to is not null) query = query.Where(s => s.Timestamp <= to);

        var data = await query
            .OrderByDescending(s => s.Timestamp)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(s => s.ToDto())
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>Get a single stored measurement.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(SensorDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorDataDto>> GetById(long id)
    {
        var reading = await _db.SensorData.AsNoTracking().FirstOrDefaultAsync(s => s.DataId == id);
        if (reading is null)
            return NotFound(new { statusCode = 404, message = $"SensorData {id} was not found." });

        return Ok(reading.ToDto());
    }

    /// <summary>
    /// Ingest a new measurement. Valid readings are stored in SensorData and update the device's
    /// LastSeen/Status; readings outside the documented range are rejected (422) and instead
    /// written to EventLog - they are never stored in SensorData (requirement D-05).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SensorDataDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SensorDataDto>> Create(SensorDataCreateDto dto)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.DeviceId == dto.DeviceId);
        if (device is null)
            return NotFound(new { statusCode = 404, message = $"Device {dto.DeviceId} was not found." });

        var timestamp = (dto.Timestamp ?? DateTime.UtcNow).ToUniversalTime();

        if (!_validator.IsValid(dto.SensorType, dto.Value, dto.Unit, out var reason))
        {
            _db.EventLogs.Add(new EventLog
            {
                DeviceId = device.DeviceId,
                Event = "InvalidMeasurement",
                Description = reason,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return UnprocessableEntity(new
            {
                statusCode = 422,
                message = "Measurement rejected: outside permitted range.",
                detail = reason
            });
        }

        var reading = new SensorData
        {
            DeviceId = dto.DeviceId,
            SensorType = dto.SensorType,
            Value = dto.Value,
            Unit = dto.Unit,
            Timestamp = timestamp
        };

        _db.SensorData.Add(reading);

        device.LastSeen = DateTime.UtcNow;
        device.Status = DeviceStatus.Online;

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = reading.DataId }, reading.ToDto());
    }

    /// <summary>Administrative delete of a single stored reading (e.g. removing bad test data).</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var reading = await _db.SensorData.FirstOrDefaultAsync(s => s.DataId == id);
        if (reading is null)
            return NotFound(new { statusCode = 404, message = $"SensorData {id} was not found." });

        _db.SensorData.Remove(reading);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
