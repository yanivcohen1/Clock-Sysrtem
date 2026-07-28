using HomeWorke.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeWorke.Api.Tests;

/// <summary>
/// Provides a fresh in-memory AppDbContext for each test.
/// Seed data is cleared to allow tests to start with a clean slate.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique DB per test
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        // Remove seed data so tests can use their own IDs
        context.Employees.RemoveRange(context.Employees);
        context.Departments.RemoveRange(context.Departments);
        context.AttendanceRecords.RemoveRange(context.AttendanceRecords);
        context.AuditLogs.RemoveRange(context.AuditLogs);
        context.SaveChanges();

        return context;
    }
}
