using System.Security.Claims;
using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeWorke.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService) =>
        _attendanceService = attendanceService;

    /// <summary>Clock In — record the start of a shift using Zurich time.</summary>
    [HttpPost("clock-in")]
    public async Task<IActionResult> ClockIn([FromBody] ClockRequest request)
    {
        var result = await _attendanceService.ClockInAsync(GetEmployeeId(), request);
        return Ok(result);
    }

    /// <summary>Clock Out — record the end of a shift using Zurich time.</summary>
    [HttpPost("clock-out")]
    public async Task<IActionResult> ClockOut([FromBody] ClockRequest request)
    {
        var result = await _attendanceService.ClockOutAsync(GetEmployeeId(), request);
        return Ok(result);
    }

    /// <summary>Get current attendance status (open shift or null).</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _attendanceService.GetCurrentStatusAsync(GetEmployeeId());
        if (result == null)
            return Ok(new { isClockedIn = false, message = "Not currently clocked in." });

        return Ok(new { isClockedIn = true, record = result });
    }

    /// <summary>Get attendance history for the authenticated employee.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _attendanceService.GetHistoryAsync(GetEmployeeId(), from, to, page, pageSize);
        return Ok(result);
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
