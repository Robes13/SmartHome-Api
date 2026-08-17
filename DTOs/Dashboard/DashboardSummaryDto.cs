using SmartHomeIoT.Api.DTOs.EventLog;

namespace SmartHomeIoT.Api.DTOs.Dashboard;

/// <summary>Powers the dashboard front page: room count, device counts, online/offline split.</summary>
public record DashboardSummaryDto(
    int TotalRooms,
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    int MeasurementsLast24Hours,
    IReadOnlyList<EventLogDto> RecentEvents
);
