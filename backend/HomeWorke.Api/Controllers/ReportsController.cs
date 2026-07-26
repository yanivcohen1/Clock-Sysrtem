using System.Security.Claims;
using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeWorke.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public ReportsController(IAttendanceService attendanceService) =>
        _attendanceService = attendanceService;

    /// <summary>
    /// Returns the visibility filter:
    /// - Admin → null (see all employees)
    /// - Manager → their own ID (see themselves + all subordinates recursively)
    /// - Employee → their own ID (see only themselves)
    /// </summary>
    private int? GetManagerFilter()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin") return null;
        return GetEmployeeId(); // Manager or Employee — sees themselves + their tree
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDailyReport(
        [FromQuery] DateTime? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var reportDate = date?.Date ?? DateTime.UtcNow.Date;
        var allRecords = await _attendanceService.GetDailyReportAsync(reportDate, GetManagerFilter());

        var totalCount = allRecords.Count;
        var present = allRecords.Count(r => r.Status == "Present");
        var absent = allRecords.Count(r => r.Status == "Absent");
        var completed = allRecords.Count(r => r.ClockOut != null);
        var avgHours = allRecords.Any(r => r.HoursWorked != null)
            ? Math.Round(allRecords.Where(r => r.HoursWorked != null).Average(r => r.HoursWorked!.Value), 2)
            : 0;

        var paged = allRecords.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            date = reportDate,
            totalCount,
            page,
            pageSize,
            presentCount = present,
            absentCount = absent,
            completedCount = completed,
            averageHours = avgHours,
            records = paged
        });
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] int year, [FromQuery] int month,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (year < 2000 || month < 1 || month > 12)
            return BadRequest(new { error = "Invalid year or month." });
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var allRecords = await _attendanceService.GetMonthlyReportAsync(year, month, GetManagerFilter());
        var totalCount = allRecords.Count;
        var paged = allRecords.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new PaginatedResponse<MonthlyReportResponse>(totalCount, page, pageSize, paged));
    }

    [HttpGet("current-status")]
    public async Task<IActionResult> GetCurrentStatus(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var allStatuses = await _attendanceService.GetCurrentStatusAllAsync(GetManagerFilter());
        var totalCount = allStatuses.Count;
        var paged = allStatuses.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            totalCount,
            page,
            pageSize,
            workingNow = allStatuses.Count(s => s.IsWorking),
            notWorking = allStatuses.Count(s => !s.IsWorking),
            employees = paged
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int? employeeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _attendanceService.GetAllHistoryAsync(
            employeeId, from, to, page, pageSize, GetManagerFilter());
        return Ok(result);
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
