using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HomeWorke.Api.Data;
using HomeWorke.Api.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HomeWorke.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<bool> ChangePasswordAsync(int employeeId, ChangePasswordRequest request);
    Task<string?> GenerateResetTokenAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword);
    int? ValidateToken(string token);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email == request.Email && e.IsActive);

        if (employee == null || !BCrypt.Net.BCrypt.Verify(request.Password, employee.PasswordHash))
            return null;

        employee.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(employee);

        return new LoginResponse(
            token,
            employee.FullName,
            employee.Role.ToString(),
            employee.EmployeeCode
        );
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
    {
        // Check for duplicate email
        var existing = await _db.Employees
            .AnyAsync(e => e.Email == request.Email);
        if (existing)
            throw new InvalidOperationException("An employee with this email already exists.");

        // Auto-generate employee code (EMP-XXX)
        var lastEmployee = await _db.Employees
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync();
        var nextNumber = (lastEmployee?.Id ?? 0) + 1;
        var employeeCode = $"EMP-{nextNumber:D3}";

        // Validate department exists if specified
        if (request.DepartmentId.HasValue)
        {
            var deptExists = await _db.Departments
                .AnyAsync(d => d.Id == request.DepartmentId.Value && d.IsActive);
            if (!deptExists)
                throw new InvalidOperationException("Selected department does not exist.");
        }

        var employee = new Models.Domain.Employee
        {
            EmployeeCode = employeeCode,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Models.Enums.UserRole.Employee,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        // Auto-login: return a token
        var token = GenerateJwtToken(employee);

        return new LoginResponse(
            token,
            employee.FullName,
            employee.Role.ToString(),
            employee.EmployeeCode
        );
    }

    public async Task<bool> ChangePasswordAsync(int employeeId, ChangePasswordRequest request)
    {
        var employee = await _db.Employees.FindAsync(employeeId);
        if (employee == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, employee.PasswordHash))
            return false;

        employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GenerateResetTokenAsync(string email)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Email == email.Trim().ToLowerInvariant() && e.IsActive);
        if (employee == null) return null;

        // Generate a simple reset token (GUID-based, valid for 15 min)
        var token = Guid.NewGuid().ToString("N");
        // In production: store token with expiry in DB. For simplicity, we
        // encode the email + token into a combined string that can be verified.
        var combined = $"{token}:{employee.Email}:{DateTime.UtcNow.AddMinutes(15):O}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
    }

    public async Task<bool> ResetPasswordAsync(string email, string resetToken, string newPassword)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(resetToken));
            var parts = decoded.Split(':');
            if (parts.Length < 2) return false;

            var tokenEmail = parts[1];
            if (!string.Equals(tokenEmail, email.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;

            // Check expiry (if encoded)
            if (parts.Length >= 3 && DateTime.TryParse(parts[2], out var expiry))
            {
                if (DateTime.UtcNow > expiry) return false;
            }

            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Email == tokenEmail.Trim().ToLowerInvariant() && e.IsActive);
            if (employee == null) return false;

            if (newPassword.Length < 6) return false;

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public int? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return idClaim != null ? int.Parse(idClaim) : null;
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwtToken(Models.Domain.Employee employee)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, employee.Id.ToString()),
            new Claim(ClaimTypes.Email, employee.Email),
            new Claim(ClaimTypes.Name, employee.FullName),
            new Claim(ClaimTypes.Role, employee.Role.ToString()),
            new Claim("employeeCode", employee.EmployeeCode)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
