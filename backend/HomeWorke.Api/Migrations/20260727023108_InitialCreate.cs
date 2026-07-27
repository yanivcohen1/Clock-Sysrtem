using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeWorke.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ShiftDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClockOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoursWorked = table.Column<double>(type: "float", nullable: true),
                    IsManuallyAdjusted = table.Column<bool>(type: "bit", nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeApiFailed = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AuditLogs",
                columns: new[] { "Id", "Action", "EntityId", "EntityName", "IpAddress", "NewValue", "OldValue", "PerformedByEmployeeId", "Timestamp" },
                values: new object[,]
                {
                    { 1, "AdminCreateEmployee", 1, "Employee", null, "{\"seed\":true,\"index\":0,\"note\":\"Demo audit entry #1\"}", null, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "ActivateEmployee", 2, "Employee", null, "{\"seed\":true,\"index\":1,\"note\":\"Demo audit entry #2\"}", null, 1, new DateTime(2026, 1, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "DeactivateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":2,\"note\":\"Demo audit entry #3\"}", null, 1, new DateTime(2026, 1, 7, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "DeleteEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":3,\"note\":\"Demo audit entry #4\"}", null, 1, new DateTime(2026, 1, 10, 3, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":4,\"note\":\"Demo audit entry #5\"}", null, 1, new DateTime(2026, 1, 13, 4, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "AdminCreateDepartment", 1, "Department", null, "{\"seed\":true,\"index\":5,\"note\":\"Demo audit entry #6\"}", null, 1, new DateTime(2026, 1, 16, 5, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "DeleteDepartment", 2, "Department", null, "{\"seed\":true,\"index\":6,\"note\":\"Demo audit entry #7\"}", null, 1, new DateTime(2026, 1, 19, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "AdminCreateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":7,\"note\":\"Demo audit entry #8\"}", null, 1, new DateTime(2026, 1, 22, 7, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "ActivateEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":8,\"note\":\"Demo audit entry #9\"}", null, 1, new DateTime(2026, 1, 25, 8, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":9,\"note\":\"Demo audit entry #10\"}", null, 1, new DateTime(2026, 1, 28, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "AdminCreateEmployee", 1, "Employee", null, "{\"seed\":true,\"index\":10,\"note\":\"Demo audit entry #11\"}", null, 1, new DateTime(2026, 1, 31, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "ActivateEmployee", 2, "Employee", null, "{\"seed\":true,\"index\":11,\"note\":\"Demo audit entry #12\"}", null, 1, new DateTime(2026, 2, 3, 11, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "DeactivateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":12,\"note\":\"Demo audit entry #13\"}", null, 1, new DateTime(2026, 2, 6, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "DeleteEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":13,\"note\":\"Demo audit entry #14\"}", null, 1, new DateTime(2026, 2, 9, 13, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":14,\"note\":\"Demo audit entry #15\"}", null, 1, new DateTime(2026, 2, 12, 14, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "AdminCreateDepartment", 1, "Department", null, "{\"seed\":true,\"index\":15,\"note\":\"Demo audit entry #16\"}", null, 1, new DateTime(2026, 2, 15, 15, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "DeleteDepartment", 2, "Department", null, "{\"seed\":true,\"index\":16,\"note\":\"Demo audit entry #17\"}", null, 1, new DateTime(2026, 2, 18, 16, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "AdminCreateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":17,\"note\":\"Demo audit entry #18\"}", null, 1, new DateTime(2026, 2, 21, 17, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "ActivateEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":18,\"note\":\"Demo audit entry #19\"}", null, 1, new DateTime(2026, 2, 24, 18, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":19,\"note\":\"Demo audit entry #20\"}", null, 1, new DateTime(2026, 2, 27, 19, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "AdminCreateEmployee", 1, "Employee", null, "{\"seed\":true,\"index\":20,\"note\":\"Demo audit entry #21\"}", null, 1, new DateTime(2026, 3, 2, 20, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "ActivateEmployee", 2, "Employee", null, "{\"seed\":true,\"index\":21,\"note\":\"Demo audit entry #22\"}", null, 1, new DateTime(2026, 3, 5, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "DeactivateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":22,\"note\":\"Demo audit entry #23\"}", null, 1, new DateTime(2026, 3, 8, 22, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "DeleteEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":23,\"note\":\"Demo audit entry #24\"}", null, 1, new DateTime(2026, 3, 11, 23, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":24,\"note\":\"Demo audit entry #25\"}", null, 1, new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "AdminCreateDepartment", 1, "Department", null, "{\"seed\":true,\"index\":25,\"note\":\"Demo audit entry #26\"}", null, 1, new DateTime(2026, 3, 18, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "DeleteDepartment", 2, "Department", null, "{\"seed\":true,\"index\":26,\"note\":\"Demo audit entry #27\"}", null, 1, new DateTime(2026, 3, 21, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "AdminCreateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":27,\"note\":\"Demo audit entry #28\"}", null, 1, new DateTime(2026, 3, 24, 3, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "ActivateEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":28,\"note\":\"Demo audit entry #29\"}", null, 1, new DateTime(2026, 3, 27, 4, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":29,\"note\":\"Demo audit entry #30\"}", null, 1, new DateTime(2026, 3, 30, 5, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "AdminCreateEmployee", 1, "Employee", null, "{\"seed\":true,\"index\":30,\"note\":\"Demo audit entry #31\"}", null, 1, new DateTime(2026, 4, 2, 6, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "ActivateEmployee", 2, "Employee", null, "{\"seed\":true,\"index\":31,\"note\":\"Demo audit entry #32\"}", null, 1, new DateTime(2026, 4, 5, 7, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "DeactivateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":32,\"note\":\"Demo audit entry #33\"}", null, 1, new DateTime(2026, 4, 8, 8, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "DeleteEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":33,\"note\":\"Demo audit entry #34\"}", null, 1, new DateTime(2026, 4, 11, 9, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":34,\"note\":\"Demo audit entry #35\"}", null, 1, new DateTime(2026, 4, 14, 10, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "AdminCreateDepartment", 1, "Department", null, "{\"seed\":true,\"index\":35,\"note\":\"Demo audit entry #36\"}", null, 1, new DateTime(2026, 4, 17, 11, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "DeleteDepartment", 2, "Department", null, "{\"seed\":true,\"index\":36,\"note\":\"Demo audit entry #37\"}", null, 1, new DateTime(2026, 4, 20, 12, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, "AdminCreateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":37,\"note\":\"Demo audit entry #38\"}", null, 1, new DateTime(2026, 4, 23, 13, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, "ActivateEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":38,\"note\":\"Demo audit entry #39\"}", null, 1, new DateTime(2026, 4, 26, 14, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":39,\"note\":\"Demo audit entry #40\"}", null, 1, new DateTime(2026, 4, 29, 15, 0, 0, 0, DateTimeKind.Utc) },
                    { 41, "AdminCreateEmployee", 1, "Employee", null, "{\"seed\":true,\"index\":40,\"note\":\"Demo audit entry #41\"}", null, 1, new DateTime(2026, 5, 2, 16, 0, 0, 0, DateTimeKind.Utc) },
                    { 42, "ActivateEmployee", 2, "Employee", null, "{\"seed\":true,\"index\":41,\"note\":\"Demo audit entry #42\"}", null, 1, new DateTime(2026, 5, 5, 17, 0, 0, 0, DateTimeKind.Utc) },
                    { 43, "DeactivateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":42,\"note\":\"Demo audit entry #43\"}", null, 1, new DateTime(2026, 5, 8, 18, 0, 0, 0, DateTimeKind.Utc) },
                    { 44, "DeleteEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":43,\"note\":\"Demo audit entry #44\"}", null, 1, new DateTime(2026, 5, 11, 19, 0, 0, 0, DateTimeKind.Utc) },
                    { 45, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":44,\"note\":\"Demo audit entry #45\"}", null, 1, new DateTime(2026, 5, 14, 20, 0, 0, 0, DateTimeKind.Utc) },
                    { 46, "AdminCreateDepartment", 1, "Department", null, "{\"seed\":true,\"index\":45,\"note\":\"Demo audit entry #46\"}", null, 1, new DateTime(2026, 5, 17, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { 47, "DeleteDepartment", 2, "Department", null, "{\"seed\":true,\"index\":46,\"note\":\"Demo audit entry #47\"}", null, 1, new DateTime(2026, 5, 20, 22, 0, 0, 0, DateTimeKind.Utc) },
                    { 48, "AdminCreateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":47,\"note\":\"Demo audit entry #48\"}", null, 1, new DateTime(2026, 5, 23, 23, 0, 0, 0, DateTimeKind.Utc) },
                    { 49, "ActivateEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":48,\"note\":\"Demo audit entry #49\"}", null, 1, new DateTime(2026, 5, 27, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 50, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":49,\"note\":\"Demo audit entry #50\"}", null, 1, new DateTime(2026, 5, 30, 1, 0, 0, 0, DateTimeKind.Utc) },
                    { 51, "AdminCreateEmployee", 1, "Employee", null, "{\"seed\":true,\"index\":50,\"note\":\"Demo audit entry #51\"}", null, 1, new DateTime(2026, 6, 2, 2, 0, 0, 0, DateTimeKind.Utc) },
                    { 52, "ActivateEmployee", 2, "Employee", null, "{\"seed\":true,\"index\":51,\"note\":\"Demo audit entry #52\"}", null, 1, new DateTime(2026, 6, 5, 3, 0, 0, 0, DateTimeKind.Utc) },
                    { 53, "DeactivateEmployee", 3, "Employee", null, "{\"seed\":true,\"index\":52,\"note\":\"Demo audit entry #53\"}", null, 1, new DateTime(2026, 6, 8, 4, 0, 0, 0, DateTimeKind.Utc) },
                    { 54, "DeleteEmployee", 4, "Employee", null, "{\"seed\":true,\"index\":53,\"note\":\"Demo audit entry #54\"}", null, 1, new DateTime(2026, 6, 11, 5, 0, 0, 0, DateTimeKind.Utc) },
                    { 55, "AdminAdjustment", 5, "AttendanceRecord", null, "{\"seed\":true,\"index\":54,\"note\":\"Demo audit entry #55\"}", null, 1, new DateTime(2026, 6, 14, 6, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Description", "IsActive", "Name" },
                values: new object[,]
                {
                    { 1, "Software Development & IT", true, "Engineering" },
                    { 2, "HR & People Operations", true, "Human Resources" },
                    { 3, "Marketing & Communications", true, "Marketing" },
                    { 4, "Finance & Accounting", true, "Finance" },
                    { 5, "Business Operations", true, "Operations" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CreatedAt", "DepartmentId", "Email", "EmployeeCode", "FirstName", "IsActive", "LastLoginAt", "LastName", "ManagerId", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "admin@homeworke.com", "EMP-ADMIN", "System", true, null, "Admin", null, "$2a$11$2gng1TarkDXhYAc3gOnI6ep6rDL/FRrEuh2Yp67PLnMDfRQ66od/C", 2 },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "manager@homeworke.com", "EMP-MGR", "Demo", true, null, "Manager", null, "$2a$11$Kdsnvh4rfAN/sUPcjQJzEuMtPXByVUi1vBdZ265yPU1MF2QxDb8Ee", 1 }
                });

            migrationBuilder.InsertData(
                table: "AttendanceRecords",
                columns: new[] { "Id", "AdjustmentReason", "ClockIn", "ClockOut", "CreatedAt", "EmployeeId", "HoursWorked", "IsManuallyAdjusted", "Notes", "ShiftDate", "Status", "TimeApiFailed", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2026, 1, 5, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 5, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(974), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 1, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 2, null, new DateTime(2026, 1, 12, 8, 10, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 12, 17, 5, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1533), 1, null, false, "Seed record for Admin — Late", new DateTime(2026, 1, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, false, null },
                    { 3, null, new DateTime(2026, 2, 3, 7, 55, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 2, 3, 16, 30, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1548), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 2, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 4, null, new DateTime(2026, 3, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1550), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 5, null, new DateTime(2026, 4, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 1, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1552), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 4, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 6, null, new DateTime(2026, 5, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 5, 10, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1578), 1, null, false, "Seed record for Admin — Late", new DateTime(2026, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, false, null },
                    { 7, null, new DateTime(2026, 6, 20, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 20, 16, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1580), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 6, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 8, null, new DateTime(2026, 6, 28, 8, 30, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1582), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 6, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 9, null, new DateTime(2026, 7, 4, 7, 50, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 4, 17, 10, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1584), 1, null, false, "Seed record for Admin — Present", new DateTime(2026, 7, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 10, null, new DateTime(2026, 7, 18, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 18, 15, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1586), 1, null, false, "Seed record for Admin — EarlyDeparture", new DateTime(2026, 7, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, false, null },
                    { 11, null, new DateTime(2026, 1, 6, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 6, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1589), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 1, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 12, null, new DateTime(2026, 2, 15, 8, 5, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 2, 15, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1592), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 2, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 13, null, new DateTime(2026, 3, 8, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 8, 16, 45, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1593), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 3, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 14, null, new DateTime(2026, 4, 12, 8, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1595), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 4, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 15, null, new DateTime(2026, 5, 5, 8, 20, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 5, 5, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1597), 2, null, false, "Seed record for Manager — Late", new DateTime(2026, 5, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, false, null },
                    { 16, null, new DateTime(2026, 5, 25, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 5, 25, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1598), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 5, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 17, null, new DateTime(2026, 6, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 10, 16, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1600), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 6, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 18, null, new DateTime(2026, 7, 1, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 1, 17, 30, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1604), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 19, null, new DateTime(2026, 7, 12, 8, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1606), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 7, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 20, null, new DateTime(2026, 7, 22, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 22, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1607), 2, null, false, "Seed record for Manager — Present", new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CreatedAt", "DepartmentId", "Email", "EmployeeCode", "FirstName", "IsActive", "LastLoginAt", "LastName", "ManagerId", "PasswordHash", "Role" },
                values: new object[] { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "demo@homeworke.com", "EMP-DEMO", "Demo", true, null, "User", 2, "$2a$11$kDpu6axhISWhoL8JJdq1p.ymYaGBq71.dAl4AsOF9yESrrPvI57s2", 0 });

            migrationBuilder.InsertData(
                table: "AttendanceRecords",
                columns: new[] { "Id", "AdjustmentReason", "ClockIn", "ClockOut", "CreatedAt", "EmployeeId", "HoursWorked", "IsManuallyAdjusted", "Notes", "ShiftDate", "Status", "TimeApiFailed", "UpdatedAt" },
                values: new object[,]
                {
                    { 21, null, new DateTime(2026, 1, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 1, 10, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1614), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 22, null, new DateTime(2026, 2, 20, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 2, 20, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1617), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 2, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 23, null, new DateTime(2026, 3, 5, 8, 15, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 5, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1619), 3, null, false, "Seed record for Employee — Late", new DateTime(2026, 3, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, false, null },
                    { 24, null, new DateTime(2026, 4, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 4, 10, 16, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1620), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 4, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 25, null, new DateTime(2026, 4, 25, 8, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1625), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 26, null, new DateTime(2026, 5, 15, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 5, 15, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1627), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 27, null, new DateTime(2026, 6, 5, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 5, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1629), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 6, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 28, null, new DateTime(2026, 6, 18, 9, 30, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 18, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1630), 3, null, false, "Seed record for Employee — Late", new DateTime(2026, 6, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), 2, false, null },
                    { 29, null, new DateTime(2026, 7, 3, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 3, 17, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1642), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 30, null, new DateTime(2026, 7, 8, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 8, 16, 30, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1648), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 31, null, new DateTime(2026, 7, 16, 8, 0, 0, 0, DateTimeKind.Unspecified), null, new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1701), 3, null, false, "Seed record for Employee — Present", new DateTime(2026, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), 0, false, null },
                    { 32, null, new DateTime(2026, 7, 25, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 25, 14, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 27, 2, 31, 5, 545, DateTimeKind.Utc).AddTicks(1703), 3, null, false, "Seed record for Employee — EarlyDeparture", new DateTime(2026, 7, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 3, false, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClockIn",
                table: "AttendanceRecords",
                column: "ClockIn");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_ShiftDate",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "ShiftDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DepartmentId",
                table: "Employees",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagerId",
                table: "Employees",
                column: "ManagerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
