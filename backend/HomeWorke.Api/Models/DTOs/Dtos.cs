namespace HomeWorke.Api.Models.DTOs;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string FullName, string Role, string EmployeeCode);

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    int? DepartmentId
);

public record ClockRequest(string? Notes);

public record AttendanceResponse(
    int Id,
    string EmployeeName,
    string EmployeeCode,
    DateTime ShiftDate,
    DateTime ClockIn,
    DateTime? ClockOut,
    double? HoursWorked,
    string Status,
    bool IsOpen,
    bool TimeApiFailed,
    bool IsManuallyAdjusted,
    string? Notes
);

public record AttendanceSummaryResponse(
    string EmployeeName,
    string EmployeeCode,
    DateTime Date,
    DateTime? ClockIn,
    DateTime? ClockOut,
    double? HoursWorked,
    string Status
);

public record DailyReportResponse(
    DateTime Date,
    int TotalEmployees,
    int PresentCount,
    int AbsentCount,
    int CompletedCount,
    double AverageHours,
    List<AttendanceSummaryResponse> Records
);

public record MonthlyReportResponse(
    int Year,
    int Month,
    string EmployeeName,
    string EmployeeCode,
    int DaysWorked,
    int DaysAbsent,
    int DaysLate,
    double TotalHours,
    double AverageDailyHours
);

public record AdminAdjustmentRequest(
    int AttendanceRecordId,
    DateTime? NewClockIn,
    DateTime? NewClockOut,
    string Reason
);

public record EmployeeDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string Email,
    string Department,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt,
    int? ManagerId = null,
    string? ManagerName = null,
    int? DepartmentId = null
);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record AdminCreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    int? DepartmentId,
    string Role, // "Employee", "Manager", or "Admin"
    int? ManagerId = null
);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);

public record AdminResetPasswordRequest(string NewPassword);

public record AdminUpdateEmployeeRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    int? DepartmentId,
    string? Role,
    int? ManagerId,
    bool? IsActive
)
{
    // Explicit parameterless constructor for JSON deserialization
    public AdminUpdateEmployeeRequest() : this(null, null, null, null, null, null, null) { }
}

public record ErrorResponse(string Message, string? Detail = null);

// ── Reports: History & Current Status ──────────

public record EmployeeStatusResponse(
    string EmployeeName,
    string EmployeeCode,
    string Department,
    bool IsWorking,
    DateTime? ClockIn,
    DateTime? ClockOut,
    double? HoursWorkedToday
);

public record PaginatedHistoryResponse(
    int TotalCount,
    int Page,
    int PageSize,
    List<AttendanceResponse> Records
);

// ── Generic pagination wrapper ─────────────────

public record PaginatedResponse<T>(
    int TotalCount,
    int Page,
    int PageSize,
    List<T> Items
);
