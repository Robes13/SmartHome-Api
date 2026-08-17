namespace SmartHomeIoT.Api.Services;

/// <summary>
/// Encodes the valid sensor ranges from the project documentation's
/// "Gyldige sensor intervaller" table. Keep this table in sync with the docs
/// if new sensor types (camera, CO2, etc.) are added later - see EK-05 / IoT-enhed backlog.
/// </summary>
public class SensorValidationService : ISensorValidationService
{
    private sealed record SensorRule(decimal Min, decimal Max, string ExpectedUnit);

    private static readonly Dictionary<string, SensorRule> Rules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["temperature"] = new SensorRule(-40m, 80m, "°C"),
        ["humidity"] = new SensorRule(0m, 100m, "%"),
        ["motion"] = new SensorRule(0m, 1m, "bool"),
        ["light"] = new SensorRule(0m, 1m, "bool"),
        ["power"] = new SensorRule(0m, 3680m, "W"),
    };

    public bool IsValid(string sensorType, decimal value, string unit, out string? reason)
    {
        if (!Rules.TryGetValue(sensorType, out var rule))
        {
            reason = $"Unknown sensor type '{sensorType}'. Known types: {string.Join(", ", Rules.Keys)}.";
            return false;
        }

        if (!string.Equals(unit, rule.ExpectedUnit, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"Unit '{unit}' does not match expected unit '{rule.ExpectedUnit}' for sensor type '{sensorType}'.";
            return false;
        }

        if (value < rule.Min || value > rule.Max)
        {
            reason = $"Value {value} is outside the permitted range [{rule.Min}, {rule.Max}] for sensor type '{sensorType}'.";
            return false;
        }

        reason = null;
        return true;
    }
}
