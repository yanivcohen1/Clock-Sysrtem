using System.Text;
using HomeWorke.Api.Data;
using HomeWorke.Api.Middleware;
using HomeWorke.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────
// Try SQL Server first; fall back to SQLite for local development
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlite = builder.Configuration.GetValue<bool>("UseSqlite");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useSqlite || string.IsNullOrEmpty(connectionString))
    {
        var sqlitePath = Path.Combine(builder.Environment.ContentRootPath, "HomeWorke.db");
        options.UseSqlite($"Data Source={sqlitePath}");
        Console.WriteLine($"📦 Using SQLite: {sqlitePath}");
    }
    else
    {
        options.UseSqlServer(connectionString);
        Console.WriteLine("📦 Using SQL Server");
    }
});

// ── Services ──────────────────────────────────────────────
builder.Services.AddScoped<ITimeService, TimeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

// ── JWT Authentication ────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // No tolerance — exact expiry
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()!;
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ── Controllers & Swagger ─────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HomeWorke Attendance API",
        Version = "v1",
        Description = "Time & Attendance system using Europe/Zurich timezone via WorldTimeAPI.org"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-migrate database ─────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var isSqlite = db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
    try
    {
        if (isSqlite)
        {
            await db.Database.EnsureCreatedAsync();
            Console.WriteLine("✅ SQLite database created/verified successfully.");
        }
        else
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("✅ Database migrated successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Database migration skipped: {ex.Message}");
        Console.WriteLine("  Run: dotnet ef database update");
    }
}

Console.WriteLine("🚀 HomeWorke API running on http://localhost:5001");
app.Run();
