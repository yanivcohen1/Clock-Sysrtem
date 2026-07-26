using System.Security.Claims;
using HomeWorke.Api.Data;
using HomeWorke.Api.Models.Domain;
using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Models.Enums;
using HomeWorke.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeWorke.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly AppDbContext _db;

    public AdminController(IAttendanceService attendanceService, AppDbContext db)
    {
        _attendanceService = attendanceService;
        _db = db;
    }

    /// <summary>Adjust an attendance record (correct mistakes).</summary>
    [HttpPut("adjust-attendance")]
    public async Task<IActionResult> AdjustAttendance([FromBody] AdminAdjustmentRequest request)
    {
        var result = await _attendanceService.AdminAdjustAsync(GetEmployeeId(), request);
        return Ok(result);
    }

    /// <summary>List all employees.</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _db.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .OrderBy(e => e.LastName)
            .Select(e => new EmployeeDto(
                e.Id,
                e.EmployeeCode,
                e.FullName,
                e.Email,
                e.Department != null ? e.Department.Name : "—",
                e.Role.ToString(),
                e.IsActive,
                e.LastLoginAt,
                e.ManagerId,
                e.Manager != null ? e.Manager.FullName : null
            ))
            .ToListAsync();

        return Ok(employees);
    }

    /// <summary>Get audit log for transparency.</summary>
    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var logs = await _db.AuditLogs
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var total = await _db.AuditLogs.CountAsync();

        return Ok(new { total, page, pageSize, logs });
    }

    /// <summary>Toggle employee active/inactive status.</summary>
    [HttpPut("employees/{id}/toggle-status")]
    public async Task<IActionResult> ToggleEmployeeStatus(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { error = "Employee not found." });

        if (employee.Id == GetEmployeeId())
            return BadRequest(new { error = "You cannot deactivate yourself." });

        employee.IsActive = !employee.IsActive;
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Employee),
            EntityId = employee.Id,
            Action = employee.IsActive ? "ActivateEmployee" : "DeactivateEmployee",
            PerformedByEmployeeId = GetEmployeeId(),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { id = employee.Id, isActive = employee.IsActive });
    }

    /// <summary>Admin creates a new employee (with role assignment).</summary>
    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee([FromBody] AdminCreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { error = "First and last name are required." });
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { error = "A valid email is required." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        var existing = await _db.Employees.AnyAsync(e => e.Email == request.Email.Trim().ToLowerInvariant());
        if (existing)
            return Conflict(new { error = "An employee with this email already exists." });

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            role = UserRole.Employee;

        // Validate manager assignment (for Employee and Manager roles)
        int? managerId = null;
        if ((role == UserRole.Employee || role == UserRole.Manager) && request.ManagerId.HasValue)
        {
            var manager = await _db.Employees.FindAsync(request.ManagerId.Value);
            if (manager == null || (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin))
                return BadRequest(new { error = "Invalid manager. Manager must have Manager or Admin role." });
            managerId = manager.Id;
        }

        var lastEmp = await _db.Employees.OrderByDescending(e => e.Id).FirstOrDefaultAsync();
        var nextNum = (lastEmp?.Id ?? 0) + 1;

        var employee = new Employee
        {
            EmployeeCode = $"EMP-{nextNum:D3}",
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            DepartmentId = request.DepartmentId,
            ManagerId = managerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Employee),
            EntityId = employee.Id,
            Action = "AdminCreateEmployee",
            PerformedByEmployeeId = GetEmployeeId(),
            NewValue = System.Text.Json.JsonSerializer.Serialize(new { employee.Email, employee.Role }),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Reload with manager for the response
        await _db.Entry(employee).Reference(e => e.Manager).LoadAsync();

        return Ok(new EmployeeDto(
            employee.Id, employee.EmployeeCode, employee.FullName, employee.Email,
            employee.Department?.Name ?? "—", employee.Role.ToString(), employee.IsActive, employee.LastLoginAt,
            employee.ManagerId,
            employee.Manager?.FullName
        ));
    }

    /// <summary>Delete an employee (hard delete).</summary>
    [HttpDelete("employees/{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { error = "Employee not found." });

        if (employee.Id == GetEmployeeId())
            return BadRequest(new { error = "You cannot delete yourself." });

        // Log before deleting
        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Employee),
            EntityId = employee.Id,
            Action = "DeleteEmployee",
            PerformedByEmployeeId = GetEmployeeId(),
            OldValue = System.Text.Json.JsonSerializer.Serialize(new { employee.FullName, employee.Email }),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Employee {employee.FullName} deleted." });
    }

    /// <summary>Admin resets an employee's password.</summary>
    [HttpPut("employees/{id}/reset-password")]
    public async Task<IActionResult> AdminResetPassword(int id, [FromBody] AdminResetPasswordRequest request)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null)
            return NotFound(new { error = "Employee not found." });

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { error = "Password must be at least 6 characters." });

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            EntityName = nameof(Employee),
            EntityId = employee.Id,
            Action = "AdminResetPassword",
            PerformedByEmployeeId = GetEmployeeId(),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Password reset for {employee.FullName}." });
    }

    private int GetEmployeeId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
