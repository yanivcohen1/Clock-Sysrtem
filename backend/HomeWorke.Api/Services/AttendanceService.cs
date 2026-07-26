using HomeWorke.Api.Data;
using HomeWorke.Api.Models.Domain;
using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeWorke.Api.Services;

public interface IAttendanceService
{
    Task<AttendanceResponse> ClockInAsync(int employeeId, ClockRequest request);
    Task<AttendanceResponse> ClockOutAsync(int employeeId, ClockRequest request);
    Task<AttendanceResponse?> GetCurrentStatusAsync(int employeeId);
    Task<List<AttendanceResponse>> GetHistoryAsync(int employeeId, DateTime? from, DateTime? to);
    Task<List<AttendanceSummaryResponse>> GetDailyReportAsync(DateTime date, int? managerId = null);
    Task<List<MonthlyReportResponse>> GetMonthlyReportAsync(int year, int month, int? managerId = null);
    Task<AttendanceResponse> AdminAdjustAsync(int adminId, AdminAdjustmentRequest request);
    Task<PaginatedHistoryResponse> GetAllHistoryAsync(int? employeeId, DateTime? from, DateTime? to, int page, int pageSize, int? managerId = null);
    Task<List<EmployeeStatusResponse>> GetCurrentStatusAllAsync(int? managerId = null);
}

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;
    private readonly ITimeService _timeService;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(AppDbContext db, ITimeService timeService, ILogger<AttendanceService> logger)
    {
        _db = db;
        _timeService = timeService;
        _logger = logger;
    }

    public async Task<AttendanceResponse> ClockInAsync(int employeeId, ClockRequest request)
    {
        var zurichTime = await _timeService.GetZurichTimeAsync();
        var zurichDate = zurichTime.Date;

        // 🔒 EDGE CASE: Check if employee already has an open record (no clock-out)
        var existingOpen = await _db.AttendanceRecords
            .AnyAsync(r => r.EmployeeId == employeeId && r.ClockOut == null);

        if (existingOpen)
            throw new InvalidOperationException(
                "You already have an active clock-in. Please clock out before clocking in again.");

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            ShiftDate = zurichDate,
            ClockIn = zurichTime,
            Notes = request.Notes,
            Status = AttendanceStatus.Present,
            CreatedAt = DateTime.UtcNow
        };

        _db.AttendanceRecords.Add(record);

        // Audit log
        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id,
            Action = "ClockIn",
            PerformedByEmployeeId = employeeId,
            NewValue = System.Text.Json.JsonSerializer.Serialize(new { record.ClockIn, record.ShiftDate }),
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var employee = await _db.Employees.FindAsync(employeeId);
        return MapToResponse(record, employee!);
    }

    public async Task<AttendanceResponse> ClockOutAsync(int employeeId, ClockRequest request)
    {
        var zurichTime = await _timeService.GetZurichTimeAsync();

        // 🔒 EDGE CASE: Find the open record
        var openRecord = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.ClockOut == null);

        if (openRecord == null)
            throw new InvalidOperationException(
                "No active clock-in found. Please clock in first.");

        // 🔒 EDGE CASE: Clock-out time is before clock-in (should not happen with external API,
        // but guard against API returning wrong times)
        if (zurichTime <= openRecord.ClockIn)
            throw new InvalidOperationException(
                "Clock-out time cannot be before or equal to clock-in time. Please try again.");

        openRecord.ClockOut = zurichTime;
        openRecord.HoursWorked = Math.Round((zurichTime - openRecord.ClockIn).TotalHours, 2);
        openRecord.UpdatedAt = DateTime.UtcNow;
        openRecord.Notes = string.IsNullOrEmpty(request.Notes)
            ? openRecord.Notes
            : $"{openRecord.Notes}; {request.Notes}";

        // 🚩 EDGE CASE: Flag shifts longer than 14 hours for admin review
        if (openRecord.HoursWorked > 14)
        {
            openRecord.Status = AttendanceStatus.Present; // Keep as present but flag
            openRecord.Notes = (openRecord.Notes ?? "") +
                " [FLAGGED: Shift exceeds 14 hours — admin review required]";
            _logger.LogWarning("Employee {EmpId} clocked out with {Hours}h — flagged for review",
                employeeId, openRecord.HoursWorked);
        }

        // Audit log
        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(AttendanceRecord),
            EntityId = openRecord.Id,
            Action = "ClockOut",
            PerformedByEmployeeId = employeeId,
            NewValue = System.Text.Json.JsonSerializer.Serialize(new
            {
                openRecord.ClockOut,
                openRecord.HoursWorked
            }),
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var employee = await _db.Employees.FindAsync(employeeId);
        return MapToResponse(openRecord, employee!);
    }

    public async Task<AttendanceResponse?> GetCurrentStatusAsync(int employeeId)
    {
        var openRecord = await _db.AttendanceRecords
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.ClockOut == null);

        return openRecord == null ? null : MapToResponse(openRecord, openRecord.Employee);
    }

    public async Task<List<AttendanceResponse>> GetHistoryAsync(
        int employeeId, DateTime? from, DateTime? to)
    {
        var query = _db.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.EmployeeId == employeeId);

        if (from.HasValue)
            query = query.Where(r => r.ShiftDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(r => r.ShiftDate <= to.Value.Date);

        var records = await query
            .OrderByDescending(r => r.ShiftDate)
            .ThenByDescending(r => r.ClockIn)
            .Take(100)
            .ToListAsync();

        return records.Select(r => MapToResponse(r, r.Employee)).ToList();
    }

    public async Task<List<AttendanceSummaryResponse>> GetDailyReportAsync(DateTime date, int? managerId = null)
    {
        // Get ALL active employees (filtered by manager hierarchy if provided), then left-join with today's attendance records
        var employeeQuery = _db.Employees.Where(e => e.IsActive);

        if (managerId.HasValue)
        {
            var subIds = await GetSubordinateIdsAsync(managerId.Value);
            employeeQuery = employeeQuery.Where(e => subIds.Contains(e.Id));
        }

        var employees = await employeeQuery
            .OrderBy(e => e.LastName)
            .ToListAsync();

        var todayRecords = await _db.AttendanceRecords
            .Where(r => r.ShiftDate == date.Date)
            .ToListAsync();

        var result = new List<AttendanceSummaryResponse>();
        foreach (var emp in employees)
        {
            var empRecords = todayRecords.Where(r => r.EmployeeId == emp.Id).ToList();

            // An employee is "Present" only if they have an open shift (clocked in, NOT clocked out)
            var openRecord = empRecords.FirstOrDefault(r => r.ClockOut == null);
            var hasOpenShift = openRecord != null;

            // Get the latest record for display (open shift takes priority)
            var displayRecord = openRecord ?? empRecords.OrderByDescending(r => r.ClockIn).FirstOrDefault();

            result.Add(new AttendanceSummaryResponse(
                emp.FullName,
                emp.EmployeeCode,
                date.Date,
                displayRecord?.ClockIn,
                displayRecord?.ClockOut,
                displayRecord?.HoursWorked,
                hasOpenShift ? "Present" : "Absent"
            ));
        }

        return result;
    }

    public async Task<List<MonthlyReportResponse>> GetMonthlyReportAsync(int year, int month, int? managerId = null)
    {
        var recordsQuery = _db.AttendanceRecords
            .Include(r => r.Employee)
            .Where(r => r.ShiftDate.Year == year && r.ShiftDate.Month == month);

        if (managerId.HasValue)
        {
            var subIds = await GetSubordinateIdsAsync(managerId.Value);
            recordsQuery = recordsQuery.Where(r => subIds.Contains(r.EmployeeId));
        }

        var records = await recordsQuery.ToListAsync(); // Bring into memory to avoid EF Core translation of computed properties

        var grouped = records
            .GroupBy(r => new { r.EmployeeId, r.Employee.FirstName, r.Employee.LastName, r.Employee.EmployeeCode })
            .Select(g => new MonthlyReportResponse(
                year,
                month,
                $"{g.Key.FirstName} {g.Key.LastName}",
                g.Key.EmployeeCode,
                g.Count(r => r.ClockOut != null),
                g.Count(r => r.Status == AttendanceStatus.Absent),
                g.Count(r => r.Status == AttendanceStatus.Late),
                Math.Round(g.Sum(r => r.HoursWorked ?? 0), 2),
                Math.Round(g.Average(r => r.HoursWorked ?? 0), 2)
            ))
            .OrderBy(r => r.EmployeeName)
            .ToList();

        return grouped;
    }

    public async Task<AttendanceResponse> AdminAdjustAsync(
        int adminId, AdminAdjustmentRequest request)
    {
        var admin = await _db.Employees.FindAsync(adminId);
        if (admin == null || admin.Role == UserRole.Employee)
            throw new UnauthorizedAccessException("Only managers and admins can adjust records.");

        var record = await _db.AttendanceRecords
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.AttendanceRecordId);

        if (record == null)
            throw new KeyNotFoundException("Attendance record not found.");

        // Store old values for audit
        var oldValue = System.Text.Json.JsonSerializer.Serialize(new
        {
            record.ClockIn,
            record.ClockOut,
            record.HoursWorked
        });

        if (request.NewClockIn.HasValue)
            record.ClockIn = request.NewClockIn.Value;
        if (request.NewClockOut.HasValue)
        {
            record.ClockOut = request.NewClockOut.Value;
            record.HoursWorked = Math.Round(
                (record.ClockOut.Value - record.ClockIn).TotalHours, 2);
        }

        record.IsManuallyAdjusted = true;
        record.AdjustmentReason = request.Reason;
        record.UpdatedAt = DateTime.UtcNow;

        // Audit log
        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(AttendanceRecord),
            EntityId = record.Id,
            Action = "AdminAdjustment",
            PerformedByEmployeeId = adminId,
            OldValue = oldValue,
            NewValue = System.Text.Json.JsonSerializer.Serialize(new
            {
                record.ClockIn,
                record.ClockOut,
                record.HoursWorked
            }),
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return MapToResponse(record, record.Employee);
    }

    public async Task<PaginatedHistoryResponse> GetAllHistoryAsync(
        int? employeeId, DateTime? from, DateTime? to, int page, int pageSize, int? managerId = null)
    {
        var query = _db.AttendanceRecords
            .Include(r => r.Employee)
            .AsQueryable();

        if (employeeId.HasValue)
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        if (managerId.HasValue)
        {
            var subIds = await GetSubordinateIdsAsync(managerId.Value);
            query = query.Where(r => subIds.Contains(r.EmployeeId));
        }
        if (from.HasValue)
            query = query.Where(r => r.ShiftDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(r => r.ShiftDate <= to.Value.Date);

        var totalCount = await query.CountAsync();

        var records = await query
            .OrderByDescending(r => r.ShiftDate)
            .ThenByDescending(r => r.ClockIn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedHistoryResponse(
            totalCount,
            page,
            pageSize,
            records.Select(r => MapToResponse(r, r.Employee)).ToList()
        );
    }

    public async Task<List<EmployeeStatusResponse>> GetCurrentStatusAllAsync(int? managerId = null)
    {
        var employeeQuery = _db.Employees.Where(e => e.IsActive);

        if (managerId.HasValue)
        {
            var subIds = await GetSubordinateIdsAsync(managerId.Value);
            employeeQuery = employeeQuery.Where(e => subIds.Contains(e.Id));
        }

        var employees = await employeeQuery
            .OrderBy(e => e.LastName)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var todayRecords = await _db.AttendanceRecords
            .Where(r => r.ShiftDate == today)
            .ToListAsync();

        var result = new List<EmployeeStatusResponse>();
        foreach (var emp in employees)
        {
            var empRecords = todayRecords.Where(r => r.EmployeeId == emp.Id).ToList();
            var openRecord = empRecords.FirstOrDefault(r => r.ClockOut == null);
            var completedToday = empRecords
                .Where(r => r.ClockOut != null)
                .Sum(r => r.HoursWorked ?? 0);

            result.Add(new EmployeeStatusResponse(
                emp.FullName,
                emp.EmployeeCode,
                emp.Department?.Name ?? "—",
                openRecord != null,
                openRecord?.ClockIn,
                openRecord?.ClockOut,
                Math.Round(completedToday, 2)
            ));
        }

        return result;
    }

    /// <summary>
    /// Returns ALL subordinate employee IDs in the management chain below the given manager,
    /// collected recursively (BFS). Does NOT include the manager themselves.
    /// </summary>
    private async Task<HashSet<int>> GetSubordinateIdsAsync(int managerId)
    {
        var allEmployees = await _db.Employees
            .Where(e => e.IsActive)
            .Select(e => new { e.Id, e.ManagerId })
            .ToListAsync();

        var result = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(managerId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var subordinates = allEmployees.Where(e => e.ManagerId == currentId).Select(e => e.Id);
            foreach (var subId in subordinates)
            {
                if (result.Add(subId))
                    queue.Enqueue(subId);
            }
        }

        return result;
    }

    private static AttendanceResponse MapToResponse(AttendanceRecord r, Employee e) =>
        new(
            r.Id,
            e.FullName,
            e.EmployeeCode,
            r.ShiftDate,
            r.ClockIn,
            r.ClockOut,
            r.HoursWorked,
            r.Status.ToString(),
            r.IsOpen,
            r.TimeApiFailed,
            r.IsManuallyAdjusted,
            r.Notes
        );
}
