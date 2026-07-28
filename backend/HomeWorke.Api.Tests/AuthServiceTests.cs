using HomeWorke.Api.Models.DTOs;
using HomeWorke.Api.Models.Domain;
using HomeWorke.Api.Models.Enums;
using HomeWorke.Api.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HomeWorke.Api.Tests;

public class AuthServiceTests
{
    private static IConfiguration CreateConfiguration(string? jwtKey = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = jwtKey ?? new string('x', 64), // at least 256-bit
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();
        return config;
    }

    // ── Login ──────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var password = "Test@123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PasswordHash = passwordHash,
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var request = new LoginRequest("john@test.com", password);

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.FullName);
        Assert.Equal("Employee", result.Role);
        Assert.Equal("EMP-001", result.EmployeeCode);
        Assert.NotNull(result.Token);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var request = new LoginRequest("john@test.com", "WrongPassword");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_InactiveEmployee_ReturnsNull()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var password = "Test@123";
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Employee,
            IsActive = false // inactive
        });
        await db.SaveChangesAsync();

        var request = new LoginRequest("john@test.com", password);

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsNull()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var request = new LoginRequest("nobody@test.com", "anything");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_UpdatesLastLoginAt()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var password = "Test@123";
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Employee,
            IsActive = true,
            LastLoginAt = null
        });
        await db.SaveChangesAsync();

        // Act
        var result = await service.LoginAsync(new LoginRequest("alice@test.com", password));

        // Assert
        Assert.NotNull(result);
        var emp = await db.Employees.FindAsync(1);
        Assert.NotNull(emp!.LastLoginAt);
    }

    // ── Register ───────────────────────────────────────

    [Fact]
    public async Task Register_ValidData_CreatesEmployeeAndReturnsToken()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var request = new RegisterRequest("Jane", "Doe", "jane@test.com", "Secure@123", null);

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Jane Doe", result.FullName);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.EmployeeCode);

        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Email == "jane@test.com");
        Assert.NotNull(emp);
        Assert.True(BCrypt.Net.BCrypt.Verify("Secure@123", emp!.PasswordHash));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Existing",
            LastName = "User",
            Email = "dupe@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var request = new RegisterRequest("New", "User", "dupe@test.com", "Secure@123", null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(request));
    }

    [Fact]
    public async Task Register_InvalidDepartment_ThrowsInvalidOperationException()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var request = new RegisterRequest("Jane", "Doe", "jane@test.com", "Secure@123", 999);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(request));
    }

    [Fact]
    public async Task Register_WithValidDepartment_Succeeds()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        // Add a department since seed data is cleared
        db.Departments.Add(new Department { Id = 1, Name = "Engineering", IsActive = true });
        await db.SaveChangesAsync();

        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var request = new RegisterRequest("Jane", "Doe", "jane@test.com", "Secure@123", 1);

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Email == "jane@test.com");
        Assert.NotNull(emp);
        Assert.Equal(1, emp!.DepartmentId);
    }

    // ── ChangePassword ─────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidCredentials_ReturnsTrue()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var oldPassword = "OldPass@123";
        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Bob",
            LastName = "Test",
            Email = "bob@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var request = new ChangePasswordRequest("OldPass@123", "NewPass@456");

        // Act
        var result = await service.ChangePasswordAsync(1, request);

        // Assert
        Assert.True(result);
        var emp = await db.Employees.FindAsync(1);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass@456", emp!.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFalse()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Bob",
            LastName = "Test",
            Email = "bob@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Correct@123"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var request = new ChangePasswordRequest("Wrong@123", "NewPass@456");

        // Act
        var result = await service.ChangePasswordAsync(1, request);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ChangePassword_NonExistentEmployee_ReturnsFalse()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var request = new ChangePasswordRequest("Anything", "NewPass@456");

        // Act
        var result = await service.ChangePasswordAsync(999, request);

        // Assert
        Assert.False(result);
    }

    // ── GenerateResetToken ─────────────────────────────

    [Fact]
    public async Task GenerateResetToken_ValidEmail_ReturnsToken()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Reset",
            LastName = "User",
            Email = "reset@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        // Act
        var token = await service.GenerateResetTokenAsync("reset@test.com");

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task GenerateResetToken_UnknownEmail_ReturnsNull()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        // Act
        var token = await service.GenerateResetTokenAsync("nobody@test.com");

        // Assert
        Assert.Null(token);
    }

    // ── ResetPassword ──────────────────────────────────

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsTrue()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Reset",
            LastName = "User",
            Email = "reset@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var token = await service.GenerateResetTokenAsync("reset@test.com");

        // Act
        var result = await service.ResetPasswordAsync("reset@test.com", token!, "NewSecure@123");

        // Assert
        Assert.True(result);
        var emp = await db.Employees.FindAsync(1);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewSecure@123", emp!.PasswordHash));
    }

    [Fact]
    public async Task ResetPassword_ShortNewPassword_ReturnsFalse()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        db.Employees.Add(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-001",
            FirstName = "Reset",
            LastName = "User",
            Email = "reset@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword"),
            Role = UserRole.Employee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var token = await service.GenerateResetTokenAsync("reset@test.com");

        // Act — password < 6 chars
        var result = await service.ResetPasswordAsync("reset@test.com", token!, "12345");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        // Act
        var result = await service.ResetPasswordAsync(
            "someone@test.com", "invalid-base64-token", "NewPass@123");

        // Assert
        Assert.False(result);
    }

    // ── ValidateToken ──────────────────────────────────

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsEmployeeId()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        var employee = new Employee
        {
            Id = 42,
            EmployeeCode = "EMP-042",
            FirstName = "Token",
            LastName = "Test",
            Email = "token@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = UserRole.Employee,
            IsActive = true
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        // Login to get a valid JWT token
        var loginResult = await service.LoginAsync(new LoginRequest("token@test.com", "password"));

        // Act
        var empId = service.ValidateToken(loginResult!.Token);

        // Assert
        Assert.Equal(42, empId);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var db = TestDbContextFactory.Create();
        var config = CreateConfiguration();
        var service = new AuthService(db, config);

        // Act
        var result = service.ValidateToken("this-is-not-a-valid-jwt");

        // Assert
        Assert.Null(result);
    }
}
