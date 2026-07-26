namespace HomeWorke.Api.Models.Domain;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty; // e.g., "AttendanceRecord"
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., "ClockIn", "ClockOut", "Adjustment"
    public int? PerformedByEmployeeId { get; set; }
    public string? OldValue { get; set; } // JSON of previous state
    public string? NewValue { get; set; } // JSON of new state
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
