namespace SmartHomeIoT.Api.Services;

public interface ISensorValidationService
{
    /// <summary>
    /// Validates a measurement against the documented valid range/unit for its sensor type
    /// (requirement D-05, table "Gyldige sensor intervaller").
    /// </summary>
    bool IsValid(string sensorType, decimal value, string unit, out string? reason);
}
