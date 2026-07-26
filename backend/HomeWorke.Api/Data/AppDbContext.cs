using HomeWorke.Api.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace HomeWorke.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee
        modelBuilder.Entity<Employee>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmployeeCode).IsUnique();
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.EmployeeCode).HasMaxLength(20).IsRequired();
        });

        // AttendanceRecord
        modelBuilder.Entity<AttendanceRecord>(ar =>
        {
            ar.HasOne(x => x.Employee)
              .WithMany(x => x.AttendanceRecords)
              .HasForeignKey(x => x.EmployeeId)
              .OnDelete(DeleteBehavior.Restrict);

            ar.HasIndex(x => new { x.EmployeeId, x.ShiftDate });
            ar.HasIndex(x => x.ClockIn);
        });

        // Department
        modelBuilder.Entity<Department>(d =>
        {
            d.HasIndex(x => x.Name).IsUnique();
            d.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(al =>
        {
            al.HasIndex(x => new { x.EntityName, x.EntityId });
            al.HasIndex(x => x.Timestamp);
        });

        // Seed default departments
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "Engineering", Description = "Software Development & IT" },
            new Department { Id = 2, Name = "Human Resources", Description = "HR & People Operations" },
            new Department { Id = 3, Name = "Marketing", Description = "Marketing & Communications" },
            new Department { Id = 4, Name = "Finance", Description = "Finance & Accounting" },
            new Department { Id = 5, Name = "Operations", Description = "Business Operations" }
        );

        // Seed admin user (password: Admin@123)
        modelBuilder.Entity<Employee>().HasData(new Employee
        {
            Id = 1,
            EmployeeCode = "EMP-ADMIN",
            FirstName = "System",
            LastName = "Admin",
            Email = "admin@homeworke.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = Models.Enums.UserRole.Admin,
            DepartmentId = 1,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed demo employee (password: Demo@123)
        modelBuilder.Entity<Employee>().HasData(new Employee
        {
            Id = 2,
            EmployeeCode = "EMP-DEMO",
            FirstName = "Demo",
            LastName = "User",
            Email = "demo@homeworke.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123"),
            Role = Models.Enums.UserRole.Employee,
            DepartmentId = 1,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
