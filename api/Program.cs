using System.Text;
using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<JwtService>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// SQL Server by default, SQLite fallback for dev (per user choice).
var conn = builder.Configuration.GetConnectionString("Default");
var useSqlServer = string.Equals(
    builder.Configuration.GetValue<string>("Database:Provider"), "SqlServer",
    StringComparison.OrdinalIgnoreCase);
if (useSqlServer && !string.IsNullOrWhiteSpace(conn))
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn));
else
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(
        builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=attendance.db"));

var jwtKey = builder.Configuration["Jwt:Key"] ?? "DEV-ONLY-CHANGE-ME-32-CHARS-MINIMUM!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AttendanceQr",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AttendanceQr",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/dashboard.html"));

// Auto-migrate + seed default users (admin/admin123, teacher/teacher123).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Teachers.AnyAsync())
    {
        db.Teachers.Add(new Api.Models.Teacher
        {
            Username = "admin", FullName = "Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "Admin"
        });
        db.Teachers.Add(new Api.Models.Teacher
        {
            Username = "teacher", FullName = "Demo Teacher",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("teacher123"), Role = "Teacher"
        });
        await db.SaveChangesAsync();
    }
}

app.Run();
