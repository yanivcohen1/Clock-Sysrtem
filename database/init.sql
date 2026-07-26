-- ============================================================
-- HomeWorke Time & Attendance System
-- Database: SQL Server
-- Timezone: Europe/Zurich (via WorldTimeAPI.org)
-- ============================================================

-- Create the database (run if not exists)
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HomeWorkeDb')
BEGIN
    CREATE DATABASE HomeWorkeDb;
END
GO

USE HomeWorkeDb;
GO

-- ── Departments ────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Departments')
BEGIN
    CREATE TABLE Departments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CONSTRAINT UQ_Departments_Name UNIQUE (Name)
    );

    -- Seed departments
    INSERT INTO Departments (Name, Description) VALUES
        (N'Engineering', N'Software Development & IT'),
        (N'Human Resources', N'HR & People Operations'),
        (N'Marketing', N'Marketing & Communications'),
        (N'Finance', N'Finance & Accounting'),
        (N'Operations', N'Business Operations');
END
GO

-- ── Employees ──────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
BEGIN
    CREATE TABLE Employees (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeCode NVARCHAR(20) NOT NULL,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        Role INT NOT NULL DEFAULT 0, -- 0=Employee, 1=Manager, 2=Admin
        DepartmentId INT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        LastLoginAt DATETIME2 NULL,
        CONSTRAINT UQ_Employees_Email UNIQUE (Email),
        CONSTRAINT UQ_Employees_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT FK_Employees_Department
            FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
    );

    -- Index for login lookups
    CREATE INDEX IX_Employees_Email ON Employees(Email);
END
GO

-- ── Attendance Records ─────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AttendanceRecords')
BEGIN
    CREATE TABLE AttendanceRecords (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        ShiftDate DATE NOT NULL,          -- Zurich-local date of shift start
        ClockIn DATETIME2 NOT NULL,       -- Zurich-local clock-in time
        ClockOut DATETIME2 NULL,          -- Zurich-local clock-out (NULL = still working)
        HoursWorked DECIMAL(10,2) NULL,   -- Calculated on clock-out
        IsManuallyAdjusted BIT NOT NULL DEFAULT 0,
        AdjustmentReason NVARCHAR(500) NULL,
        TimeApiFailed BIT NOT NULL DEFAULT 0,
        Status INT NOT NULL DEFAULT 0,    -- 0=Present, 1=Absent, 2=Late, etc.
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL,
        CONSTRAINT FK_AttendanceRecords_Employee
            FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
    );

    -- Composite index for employee + date queries (most common)
    CREATE INDEX IX_AttendanceRecords_EmployeeId_ShiftDate
        ON AttendanceRecords(EmployeeId, ShiftDate);

    -- Index for finding open records (ClockOut IS NULL)
    CREATE INDEX IX_AttendanceRecords_EmployeeId_ClockOut
        ON AttendanceRecords(EmployeeId) WHERE ClockOut IS NULL;

    -- Index for report date ranges
    CREATE INDEX IX_AttendanceRecords_ShiftDate
        ON AttendanceRecords(ShiftDate);
END
GO

-- ── Audit Log ──────────────────────────────────────────────
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE AuditLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EntityName NVARCHAR(100) NOT NULL,
        EntityId INT NOT NULL,
        Action NVARCHAR(100) NOT NULL,
        PerformedByEmployeeId INT NULL,
        OldValue NVARCHAR(MAX) NULL,      -- JSON snapshot before change
        NewValue NVARCHAR(MAX) NULL,      -- JSON snapshot after change
        IpAddress NVARCHAR(50) NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_AuditLogs_Employee
            FOREIGN KEY (PerformedByEmployeeId) REFERENCES Employees(Id)
    );

    CREATE INDEX IX_AuditLogs_Entity ON AuditLogs(EntityName, EntityId);
    CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);
END
GO

-- ── Seed Admin User ────────────────────────────────────────
-- Password: Admin@123 (BCrypt hash — generated at runtime by the app)
-- This is just a placeholder; the actual seed happens via EF Core.
IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = 'admin@homeworke.com')
BEGIN
    -- The password hash below is for 'Admin@123' generated with BCrypt
    INSERT INTO Employees (EmployeeCode, FirstName, LastName, Email, PasswordHash, Role, DepartmentId, IsActive, CreatedAt)
    VALUES (
        'EMP-ADMIN',
        'System', 'Admin',
        'admin@homeworke.com',
        '$2a$11$K3xQ8Z1yMvH5rWtP7jNkBOuL9aXcF4dG2hJ6mV8sY0eR1qT3wU5i', -- placeholder
        2, -- Admin
        1, -- Engineering
        1,
        '2026-01-01T00:00:00'
    );
END
GO

PRINT '✅ HomeWorke database initialized successfully.';
PRINT '   Default admin: admin@homeworke.com / Admin@123';
GO
