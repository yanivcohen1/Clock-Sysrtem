using HomeWorke.Api.Models.Enums;

namespace HomeWorke.Api.Models.Domain;

public class AttendanceRecord
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Zurich-local date of the shift start.</summary>
    public DateTime ShiftDate { get; set; }

    /// <summary>Zurich-local time when employee clocked in.</summary>
    public DateTime ClockIn { get; set; }

    /// <summary>Zurich-local time when employee clocked out (null if still working).</summary>
    public DateTime? ClockOut { get; set; }

    /// <summary>Total hours worked (calculated on clock-out).</summary>
    public double? HoursWorked { get; set; }

    /// <summary>Indicates if this record was manually modified by an admin.</summary>
    public bool IsManuallyAdjusted { get; set; } = false;

    public string? AdjustmentReason { get; set; }

    /// <summary>Indicates if the time API was unavailable during clock-in/out.</summary>
    public bool TimeApiFailed { get; set; } = false;

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Computed
    public bool IsOpen => ClockOut == null;
    public TimeSpan? Duration => ClockOut.HasValue ? ClockOut.Value - ClockIn : null;
}
