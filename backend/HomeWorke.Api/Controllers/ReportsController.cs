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
    public async Task<IActionResult> GetDailyReport([FromQuery] DateTime? date)
    {
        var reportDate = date?.Date ?? DateTime.UtcNow.Date;
        var records = await _attendanceService.GetDailyReportAsync(reportDate, GetManagerFilter());

        var present = records.Count(r => r.Status == "Present");
        var absent = records.Count(r => r.Status == "Absent");
        var completed = records.Count(r => r.ClockOut != null);
        var avgHours = records.Any(r => r.HoursWorked != null)
            ? Math.Round(records.Where(r => r.HoursWorked != null).Average(r => r.HoursWorked!.Value), 2)
            : 0;

        return Ok(new DailyReportResponse(
            reportDate, records.Count, present, absent, completed, avgHours, records));
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 2000 || month < 1 || month > 12)
            return BadRequest(new { error = "Invalid year or month." });

        var records = await _attendanceService.GetMonthlyReportAsync(year, month, GetManagerFilter());
        return Ok(records);
    }

    [HttpGet("current-status")]
    public async Task<IActionResult> GetCurrentStatus()
    {
        var statuses = await _attendanceService.GetCurrentStatusAllAsync(GetManagerFilter());
        return Ok(new
        {
            totalEmployees = statuses.Count,
            workingNow = statuses.Count(s => s.IsWorking),
            notWorking = statuses.Count(s => !s.IsWorking),
            employees = statuses
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int? employeeId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await _attendanceService.GetAllHistoryAsync(
            employeeId, from, to, page, pageSize, GetManagerFilter());
        return Ok(result);
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
