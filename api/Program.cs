using System.Text;
using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Consistent validation error shape: { message, errors }.
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());
            return new BadRequestObjectResult(new { message = "Validation failed.", errors });
        };
    });
builder.Services.AddOpenApi();
builder.Services.AddScoped<JwtService>();

// CORS: explicit origins from Cors:AllowedOrigins. Development with no
// configured origins falls back to AllowAnyOrigin (logged warning);
// production with no configured origins disables CORS entirely.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];
var corsConfigured = allowedOrigins.Length > 0;
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (corsConfigured)
        p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    else if (builder.Environment.IsDevelopment())
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    else
        p.WithOrigins().AllowAnyHeader().AllowAnyMethod(); // no origins: same-origin only
}));

// SQL Server when Database:Provider=SqlServer, else SQLite file.
var conn = builder.Configuration.GetConnectionString("Default");
var useSqlServer = string.Equals(
    builder.Configuration.GetValue<string>("Database:Provider"), "SqlServer",
    StringComparison.OrdinalIgnoreCase);
if (useSqlServer && !string.IsNullOrWhiteSpace(conn))
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(conn));
else
    builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(
        builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=attendance.db"));

// JWT key must come from user-secrets / appsettings.Development.json locally
// and from the Jwt__Key environment variable in production — never committed.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("CHANGE-ME"))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException(
            "Jwt:Key is not configured. Set the Jwt__Key environment variable to a long random secret (>=32 chars).");
    jwtKey = "DEV-ONLY-LOCAL-KEY-NOT-FOR-PRODUCTION-USE-00000000";
}
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
{
    app.MapOpenApi();
    if (!corsConfigured)
        app.Logger.LogWarning("CORS: no Cors:AllowedOrigins configured; allowing any origin (development only).");
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
    if (!corsConfigured)
        app.Logger.LogWarning("CORS: no Cors:AllowedOrigins configured; cross-origin requests will be rejected.");
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/dashboard.html"));

// Auto-create/migrate the database.
// SQLite (local dev): apply EF Core migrations (scaffolded for SQLite).
// SQL Server (production): create the schema directly on first boot. The bundled
// migrations are SQLite-flavored and SQL Server needs its own type mapping, so
// production schema evolution is done via reviewed SQL from
// `dotnet ef migrations script` (run with Database__Provider=SqlServer).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useSqlServer)
    {
        await db.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Ensured SQL Server database schema exists.");
    }
    else
    {
        await db.Database.MigrateAsync();
    }
    var seedDemo = builder.Configuration.GetValue<bool?>("SeedDemoUsers")
        ?? app.Environment.IsDevelopment();
    if (seedDemo && !await db.Teachers.AnyAsync())
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
        app.Logger.LogWarning("Seeded DEVELOPMENT demo users (admin/teacher). Never enable SeedDemoUsers in production.");
    }
}

app.Run();
