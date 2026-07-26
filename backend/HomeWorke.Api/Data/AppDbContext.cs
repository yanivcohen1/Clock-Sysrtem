using HomeWorke.Api.Models.Domain;
using HomeWorke.Api.Models.Enums;
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

            // Self-referencing: Employee → Manager
            e.HasOne(x => x.Manager)
             .WithMany(x => x.Subordinates)
             .HasForeignKey(x => x.ManagerId)
             .OnDelete(DeleteBehavior.Restrict);
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

        // Seed demo manager (password: Manager@123)
        modelBuilder.Entity<Employee>().HasData(new Employee
        {
            Id = 2,
            EmployeeCode = "EMP-MGR",
            FirstName = "Demo",
            LastName = "Manager",
            Email = "manager@homeworke.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
            Role = Models.Enums.UserRole.Manager,
            DepartmentId = 1,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // Seed demo employee (password: Demo@123) — managed by Demo Manager
        modelBuilder.Entity<Employee>().HasData(new Employee
        {
            Id = 3,
            EmployeeCode = "EMP-DEMO",
            FirstName = "Demo",
            LastName = "User",
            Email = "demo@homeworke.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123"),
            Role = Models.Enums.UserRole.Employee,
            DepartmentId = 1,
            ManagerId = 2, // Managed by Demo Manager (ID=2)
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        // ── Seed attendance records (32 records so pagination triggers at pageSize=10) ──
        var seedRecords = new List<AttendanceRecord>();
        int recordId = 1;
        // Employee 1 (Admin): 10 records, Jan–Jul 2026, mixed statuses
        (DateTime shiftDate, DateTime clockIn, DateTime? clockOut, AttendanceStatus status)[] adminDates = new[] {
            (new DateTime(2026,1,5), new DateTime(2026,1,5,8,0,0), new DateTime(2026,1,5,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,1,12), new DateTime(2026,1,12,8,10,0), new DateTime(2026,1,12,17,5,0), AttendanceStatus.Late),
            (new DateTime(2026,2,3), new DateTime(2026,2,3,7,55,0), new DateTime(2026,2,3,16,30,0), AttendanceStatus.Present),
            (new DateTime(2026,3,15), new DateTime(2026,3,15,8,0,0), (DateTime?)null, AttendanceStatus.Present),
            (new DateTime(2026,4,1), new DateTime(2026,4,1,8,0,0), new DateTime(2026,4,1,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,5,10), new DateTime(2026,5,10,9,0,0), new DateTime(2026,5,10,17,0,0), AttendanceStatus.Late),
            (new DateTime(2026,6,20), new DateTime(2026,6,20,8,0,0), new DateTime(2026,6,20,16,0,0), AttendanceStatus.Present),
            (new DateTime(2026,6,28), new DateTime(2026,6,28,8,30,0), (DateTime?)null, AttendanceStatus.Present),
            (new DateTime(2026,7,4), new DateTime(2026,7,4,7,50,0), new DateTime(2026,7,4,17,10,0), AttendanceStatus.Present),
            (new DateTime(2026,7,18), new DateTime(2026,7,18,8,0,0), new DateTime(2026,7,18,15,0,0), AttendanceStatus.EarlyDeparture),
        };
        // Employee 2 (Manager): 10 records
        (DateTime shiftDate, DateTime clockIn, DateTime? clockOut, AttendanceStatus status)[] mgrDates = new[] {
            (new DateTime(2026,1,6), new DateTime(2026,1,6,8,0,0), new DateTime(2026,1,6,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,2,15), new DateTime(2026,2,15,8,5,0), new DateTime(2026,2,15,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,3,8), new DateTime(2026,3,8,8,0,0), new DateTime(2026,3,8,16,45,0), AttendanceStatus.Present),
            (new DateTime(2026,4,12), new DateTime(2026,4,12,8,0,0), (DateTime?)null, AttendanceStatus.Present),
            (new DateTime(2026,5,5), new DateTime(2026,5,5,8,20,0), new DateTime(2026,5,5,17,0,0), AttendanceStatus.Late),
            (new DateTime(2026,5,25), new DateTime(2026,5,25,8,0,0), new DateTime(2026,5,25,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,6,10), new DateTime(2026,6,10,8,0,0), new DateTime(2026,6,10,16,0,0), AttendanceStatus.Present),
            (new DateTime(2026,7,1), new DateTime(2026,7,1,8,0,0), new DateTime(2026,7,1,17,30,0), AttendanceStatus.Present),
            (new DateTime(2026,7,12), new DateTime(2026,7,12,8,0,0), (DateTime?)null, AttendanceStatus.Present),
            (new DateTime(2026,7,22), new DateTime(2026,7,22,8,0,0), new DateTime(2026,7,22,17,0,0), AttendanceStatus.Present),
        };
        // Employee 3 (Demo User): 12 records
        (DateTime shiftDate, DateTime clockIn, DateTime? clockOut, AttendanceStatus status)[] empDates = new[] {
            (new DateTime(2026,1,10), new DateTime(2026,1,10,8,0,0), new DateTime(2026,1,10,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,2,20), new DateTime(2026,2,20,8,0,0), new DateTime(2026,2,20,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,3,5), new DateTime(2026,3,5,8,15,0), new DateTime(2026,3,5,17,0,0), AttendanceStatus.Late),
            (new DateTime(2026,4,10), new DateTime(2026,4,10,8,0,0), new DateTime(2026,4,10,16,0,0), AttendanceStatus.Present),
            (new DateTime(2026,4,25), new DateTime(2026,4,25,8,0,0), (DateTime?)null, AttendanceStatus.Present),
            (new DateTime(2026,5,15), new DateTime(2026,5,15,8,0,0), new DateTime(2026,5,15,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,6,5), new DateTime(2026,6,5,8,0,0), new DateTime(2026,6,5,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,6,18), new DateTime(2026,6,18,9,30,0), new DateTime(2026,6,18,17,0,0), AttendanceStatus.Late),
            (new DateTime(2026,7,3), new DateTime(2026,7,3,8,0,0), new DateTime(2026,7,3,17,0,0), AttendanceStatus.Present),
            (new DateTime(2026,7,8), new DateTime(2026,7,8,8,0,0), new DateTime(2026,7,8,16,30,0), AttendanceStatus.Present),
            (new DateTime(2026,7,16), new DateTime(2026,7,16,8,0,0), (DateTime?)null, AttendanceStatus.Present),
            (new DateTime(2026,7,25), new DateTime(2026,7,25,8,0,0), new DateTime(2026,7,25,14,0,0), AttendanceStatus.EarlyDeparture),
        };
        foreach (var (shiftDate, clockIn, clockOut, status) in adminDates)
            seedRecords.Add(new AttendanceRecord { Id = recordId++, EmployeeId = 1, ShiftDate = shiftDate, ClockIn = clockIn, ClockOut = clockOut, Status = status, Notes = $"Seed record for Admin — {status}" });
        foreach (var (shiftDate, clockIn, clockOut, status) in mgrDates)
            seedRecords.Add(new AttendanceRecord { Id = recordId++, EmployeeId = 2, ShiftDate = shiftDate, ClockIn = clockIn, ClockOut = clockOut, Status = status, Notes = $"Seed record for Manager — {status}" });
        foreach (var (shiftDate, clockIn, clockOut, status) in empDates)
            seedRecords.Add(new AttendanceRecord { Id = recordId++, EmployeeId = 3, ShiftDate = shiftDate, ClockIn = clockIn, ClockOut = clockOut, Status = status, Notes = $"Seed record for Employee — {status}" });
        modelBuilder.Entity<AttendanceRecord>().HasData(seedRecords.ToArray());

        // ── Seed audit logs (55 entries so pagination triggers at pageSize=10) ──
        var seedAuditLogs = new List<AuditLog>();
        int auditId = 1;
        var auditBaseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string[] entities = { "Employee", "Employee", "Employee", "Employee", "AttendanceRecord", "Department", "Department", "Employee", "Employee", "AttendanceRecord" };
        string[] actions = { "AdminCreateEmployee", "ActivateEmployee", "DeactivateEmployee", "DeleteEmployee", "AdminAdjustment", "AdminCreateDepartment", "DeleteDepartment", "AdminCreateEmployee", "ActivateEmployee", "AdminAdjustment" };
        for (int i = 0; i < 55; i++)
        {
            seedAuditLogs.Add(new AuditLog
            {
                Id = auditId++,
                EntityName = entities[i % entities.Length],
                EntityId = (i % 5) + 1,
                Action = actions[i % actions.Length],
                PerformedByEmployeeId = 1, // Admin did it
                NewValue = $"{{\"seed\":true,\"index\":{i},\"note\":\"Demo audit entry #{i+1}\"}}",
                Timestamp = auditBaseTime.AddDays(i * 3).AddHours(i)
            });
        }
        modelBuilder.Entity<AuditLog>().HasData(seedAuditLogs.ToArray());
    }
}
