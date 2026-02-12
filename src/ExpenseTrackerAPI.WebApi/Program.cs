using Serilog;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.Infrastructure.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration));

// Configure Database Connection
var connectionString = GetConnectionString(builder.Configuration, builder.Environment);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure JWT Settings
ConfigureJwtSettings(builder.Services, builder.Configuration, builder.Environment);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add request logging
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting ExpenseTracker API");

    // Auto-migrate and seed database on startup
    await InitializeDatabaseAsync(app.Services);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static string GetConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
{
    if (environment.IsProduction())
    {
        // In production, use environment variables
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "expense_tracker_db";
        var username = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? throw new InvalidOperationException("DB_PASSWORD environment variable is required in production");
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var sslMode = Environment.GetEnvironmentVariable("DB_SSL_MODE") ?? "Prefer";

        return $"Host={host};Database={database};Username={username};Password={password};Port={port};SSL Mode={sslMode};";
    }
    else
    {
        // In development/local, use appsettings.json
        return configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found in configuration");
    }
}

static void ConfigureJwtSettings(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
{
    if (environment.IsProduction())
    {
        // In production, override JWT settings with environment variables
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable is required in production");
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "ExpenseTrackerAPI";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ExpenseTrackerAPI";
        var jwtExpiryHours = Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") ?? "24";

        // Override configuration values
        configuration["JwtSettings:SecretKey"] = jwtSecret;
        configuration["JwtSettings:Issuer"] = jwtIssuer;
        configuration["JwtSettings:Audience"] = jwtAudience;
        configuration["JwtSettings:ExpiryInHours"] = jwtExpiryHours;
    }

    // JWT settings will be configured later when authentication is added
    // For now, just validate that settings exist
    var secretKey = configuration["JwtSettings:SecretKey"];
    if (string.IsNullOrEmpty(secretKey))
    {
        throw new InvalidOperationException("JWT SecretKey is not configured");
    }
}

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        Log.Information("Checking database connectivity...");

        // Test database connection
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            Log.Warning("Cannot connect to database");
            return;
        }

        Log.Information("Database connection successful");

        // Apply pending migrations automatically
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            Log.Information("Applying {Count} pending migrations...", pendingMigrations.Count());
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        else
        {
            Log.Information("Database is up to date - no pending migrations");
        }

        // Seed database if empty
        if (!await dbContext.Users.AnyAsync())
        {
            Log.Information("Database is empty - seeding with initial data...");
            await DatabaseSeeder.SeedIfEmptyAsync(dbContext);
            Log.Information("Database seeded successfully");
        }
        else
        {
            Log.Information("Database already contains data - skipping seeding");
        }

        Log.Information("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed");
        throw; // Re-throw to prevent app startup with broken database
    }
}
