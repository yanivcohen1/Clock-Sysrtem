using HomeWorke.Api.Data;
using HomeWorke.Api.Models.Domain;
using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Models.Enums;
using HomeWorke.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HomeWorke.Api.Tests;

public class AttendanceServiceTests
{
    private static (AppDbContext, IAttendanceService) CreateService(
        Mock<ITimeService>? timeServiceMock = null)
    {
        var db = TestDbContextFactory.Create();

        if (timeServiceMock == null)
        {
            timeServiceMock = new Mock<ITimeService>();
            timeServiceMock.Setup(t => t.GetZurichTimeAsync())
                .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0)); // 9:00 Zurich
        }

        var logger = new Mock<ILogger<AttendanceService>>().Object;
        var service = new AttendanceService(db, timeServiceMock.Object, logger);

        return (db, service);
    }

    private static async Task SeedEmployee(AppDbContext db, int id = 1,
        string code = "EMP-001", string firstName = "John", string lastName = "Doe",
        UserRole role = UserRole.Employee)
    {
        db.Employees.Add(new Employee
        {
            Id = id,
            EmployeeCode = code,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLower()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = role,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    // ── ClockIn ────────────────────────────────────────

    [Fact]
    public async Task ClockIn_CreatesAttendanceRecord()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db);

        // Act
        var result = await service.ClockInAsync(1, new ClockRequest("Morning shift"));

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.EmployeeName);
        Assert.Equal("EMP-001", result.EmployeeCode);
        Assert.True(result.IsOpen);
        Assert.False(result.TimeApiFailed);
        Assert.False(result.IsManuallyAdjusted);
        Assert.Equal("Present", result.Status);

        var record = await db.AttendanceRecords.FirstOrDefaultAsync();
        Assert.NotNull(record);
        Assert.Equal(1, record!.EmployeeId);
        Assert.Equal("Morning shift", record.Notes);

        // Audit log should be created
        var auditLog = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("ClockIn", auditLog!.Action);
    }

    [Fact]
    public async Task ClockIn_AlreadyClockedIn_ThrowsInvalidOperationException()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db);

        // First clock-in
        await service.ClockInAsync(1, new ClockRequest(null));

        // Act & Assert - second clock-in should fail
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ClockInAsync(1, new ClockRequest(null)));
    }

    // ── ClockOut ───────────────────────────────────────

    [Fact]
    public async Task ClockOut_ClosesOpenRecord()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.SetupSequence(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0))   // ClockIn at 9:00
            .ReturnsAsync(new DateTime(2026, 7, 28, 17, 0, 0)); // ClockOut at 17:00

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db);

        await service.ClockInAsync(1, new ClockRequest("Morning"));

        // Act
        var result = await service.ClockOutAsync(1, new ClockRequest("Done"));

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsOpen);
        Assert.Equal(8.0, result.HoursWorked); // 8 hours

        var record = await db.AttendanceRecords.FindAsync(result.Id);
        Assert.NotNull(record!.ClockOut);
        Assert.Equal(8.0, record.HoursWorked);
        Assert.Contains("Done", record.Notes);
    }

    [Fact]
    public async Task ClockOut_NoActiveRecord_ThrowsInvalidOperationException()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ClockOutAsync(1, new ClockRequest(null)));
    }

    [Fact]
    public async Task ClockOut_ClockOutBeforeClockIn_ThrowsInvalidOperationException()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.SetupSequence(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 17, 0, 0))  // ClockIn at 17:00
            .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0));  // ClockOut at 9:00 (before!)

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db);

        await service.ClockInAsync(1, new ClockRequest(null));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ClockOutAsync(1, new ClockRequest(null)));
    }

    [Fact]
    public async Task ClockOut_ShiftOver14Hours_FlagsForReview()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.SetupSequence(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 6, 0, 0))    // ClockIn at 6:00
            .ReturnsAsync(new DateTime(2026, 7, 28, 22, 0, 0));  // ClockOut at 22:00 (16h)

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db);

        await service.ClockInAsync(1, new ClockRequest(null));

        // Act
        var result = await service.ClockOutAsync(1, new ClockRequest(null));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(16.0, result.HoursWorked);

        var record = await db.AttendanceRecords.FindAsync(result.Id);
        Assert.Contains("FLAGGED", record!.Notes);
    }

    // ── GetCurrentStatus ───────────────────────────────

    [Fact]
    public async Task GetCurrentStatus_WhenClockedIn_ReturnsRecord()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db);
        await service.ClockInAsync(1, new ClockRequest(null));

        // Act
        var status = await service.GetCurrentStatusAsync(1);

        // Assert
        Assert.NotNull(status);
        Assert.True(status!.IsOpen);
    }

    [Fact]
    public async Task GetCurrentStatus_WhenNotClockedIn_ReturnsNull()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db);

        // Act
        var status = await service.GetCurrentStatusAsync(1);

        // Assert
        Assert.Null(status);
    }

    // ── GetHistory ─────────────────────────────────────

    [Fact]
    public async Task GetHistory_ReturnsPaginatedRecords()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.Setup(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0));

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db);

        // Create a few complete records manually (via clock-in + clock-out)
        for (int i = 1; i <= 5; i++)
        {
            var record = new AttendanceRecord
            {
                Id = i,
                EmployeeId = 1,
                ShiftDate = new DateTime(2026, 7, 20 + i),
                ClockIn = new DateTime(2026, 7, 20 + i, 9, 0, 0),
                ClockOut = new DateTime(2026, 7, 20 + i, 17, 0, 0),
                HoursWorked = 8,
                Status = AttendanceStatus.Present,
                CreatedAt = DateTime.UtcNow
            };
            db.AttendanceRecords.Add(record);
        }
        await db.SaveChangesAsync();

        // Act
        var history = await service.GetHistoryAsync(1, null, null, 1, 3);

        // Assert
        Assert.Equal(3, history.Count); // page size = 3
    }

    [Fact]
    public async Task GetHistory_PaginationDefaults_Applied()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db);

        // Act — no args should use defaults (page=1, pageSize=10)
        var history = await service.GetHistoryAsync(1, null, null);

        // Assert
        Assert.NotNull(history);
    }

    [Fact]
    public async Task GetHistory_DateFilter_Works()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.Setup(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0));

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db);

        // Records on July 25 and July 30
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = 1, EmployeeId = 1, ShiftDate = new DateTime(2026, 7, 25),
            ClockIn = new DateTime(2026, 7, 25, 9, 0, 0),
            ClockOut = new DateTime(2026, 7, 25, 17, 0, 0),
            HoursWorked = 8, Status = AttendanceStatus.Present
        });
        db.AttendanceRecords.Add(new AttendanceRecord
        {
            Id = 2, EmployeeId = 1, ShiftDate = new DateTime(2026, 7, 30),
            ClockIn = new DateTime(2026, 7, 30, 9, 0, 0),
            HoursWorked = null, Status = AttendanceStatus.Present // still open
        });
        await db.SaveChangesAsync();

        // Act — filter July 24 - 26 only
        var history = await service.GetHistoryAsync(1,
            new DateTime(2026, 7, 24), new DateTime(2026, 7, 26));

        // Assert
        Assert.Single(history);
        Assert.Equal(new DateTime(2026, 7, 25), history[0].ShiftDate);
    }

    // ── AdminAdjust ────────────────────────────────────

    [Fact]
    public async Task AdminAdjust_UpdatesRecordAndAuditLogs()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.SetupSequence(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0))
            .ReturnsAsync(new DateTime(2026, 7, 28, 17, 0, 0));

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db, 1, "EMP-001", "John", "Doe", UserRole.Employee);
        await SeedEmployee(db, 2, "EMP-002", "Admin", "User", UserRole.Admin); // admin

        var record = await service.ClockInAsync(1, new ClockRequest(null));

        var adjustRequest = new AdminAdjustmentRequest(
            record.Id,
            new DateTime(2026, 7, 28, 8, 0, 0),  // new clock-in
            new DateTime(2026, 7, 28, 18, 0, 0),  // new clock-out
            "Corrected time"
        );

        // Act
        var adjusted = await service.AdminAdjustAsync(2, adjustRequest);

        // Assert
        Assert.NotNull(adjusted);
        Assert.True(adjusted.IsManuallyAdjusted);
        Assert.Equal(new DateTime(2026, 7, 28, 8, 0, 0), adjusted.ClockIn);
        Assert.Equal(new DateTime(2026, 7, 28, 18, 0, 0), adjusted.ClockOut);
        Assert.Equal(10.0, adjusted.HoursWorked);

        // Audit logs: AdminAdjustment should be logged
        var auditLogs = await db.AuditLogs
            .Where(l => l.Action == "AdminAdjustment")
            .ToListAsync();
        Assert.Single(auditLogs);

        // The adjustment reason is stored on the record itself
        var updatedRecord = await db.AttendanceRecords.FindAsync(record.Id);
        Assert.Equal("Corrected time", updatedRecord!.AdjustmentReason);
    }

    // ── GetCurrentStatusAll ────────────────────────────

    [Fact]
    public async Task GetCurrentStatusAll_ReturnsAllActiveEmployees()
    {
        // Arrange
        var (db, service) = CreateService();
        await SeedEmployee(db, 1, "EMP-001", "Alice", "One");
        await SeedEmployee(db, 2, "EMP-002", "Bob", "Two");

        // Alice is clocked in
        await service.ClockInAsync(1, new ClockRequest(null));

        // Act
        var allStatus = await service.GetCurrentStatusAllAsync();

        // Assert
        Assert.Equal(2, allStatus.Count);
        Assert.Contains(allStatus, s => s.EmployeeCode == "EMP-001" && s.IsWorking);
        Assert.Contains(allStatus, s => s.EmployeeCode == "EMP-002" && !s.IsWorking);
    }

    // ── GetDailyReport ─────────────────────────────────

    [Fact]
    public async Task GetDailyReport_ReturnsReportForDate()
    {
        // Arrange
        var timeMock = new Mock<ITimeService>();
        timeMock.Setup(t => t.GetZurichTimeAsync())
            .ReturnsAsync(new DateTime(2026, 7, 28, 9, 0, 0));

        var (db, service) = CreateService(timeMock);
        await SeedEmployee(db);

        await service.ClockInAsync(1, new ClockRequest(null));

        // Act
        var report = await service.GetDailyReportAsync(new DateTime(2026, 7, 28));

        // Assert
        Assert.Single(report); // only one employee
        Assert.Equal("John Doe", report[0].EmployeeName);
        Assert.NotNull(report[0].ClockIn);
        Assert.Null(report[0].ClockOut); // not clocked out yet
        Assert.Null(report[0].HoursWorked);
    }
}
