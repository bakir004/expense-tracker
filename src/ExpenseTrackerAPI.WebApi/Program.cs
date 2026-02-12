using Serilog;
using Microsoft.EntityFrameworkCore;
using ExpenseTrackerAPI.Infrastructure.Persistence;
using ExpenseTrackerAPI.Infrastructure.Shared;
using ExpenseTrackerAPI.WebApi.Extensions;
using ExpenseTrackerAPI.WebApi.Middleware;

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

// Configure routing options for lowercase URLs
builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

// Add RFC 9110 compliant Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = (context) =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        // Add RFC 9110 compliance type URIs
        if (context.ProblemDetails.Status.HasValue)
        {
            context.ProblemDetails.Type = context.ProblemDetails.Status switch
            {
                400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                422 => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
                500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                _ => "https://tools.ietf.org/html/rfc9110"
            };
        }
    };
});

builder.Services.AddEndpointsApiExplorer();

// Add API versioning using our extension method
DependencyInjectionExtensions.AddApiVersioning(builder.Services);

// Add Swagger with versioning support
builder.Services.AddSwaggerGen(c =>
{
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Swagger for API versioning
builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();

// Add application and infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

// Add JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Add request logging
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();

        // Create a Swagger endpoint for each API version
        foreach (var description in provider.ApiVersionDescriptions.Reverse())
        {
            c.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"ExpenseTracker API {description.GroupName.ToUpperInvariant()}");
        }

        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableValidator();
        c.ShowExtensions();
        c.EnableFilter();
    });
}

app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Add user context middleware (must be after UseAuthentication)
app.UseUserContext();

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
        configuration["Jwt:SecretKey"] = jwtSecret;
        configuration["Jwt:Issuer"] = jwtIssuer;
        configuration["Jwt:Audience"] = jwtAudience;
        configuration["Jwt:ExpirationHours"] = jwtExpiryHours;
    }

    // JWT settings will be configured later when authentication is added
    // For now, just validate that settings exist
    var secretKey = configuration["Jwt:SecretKey"];
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

/// <summary>
/// Configures Swagger options for API versioning
/// </summary>
public class ConfigureSwaggerOptions : Microsoft.Extensions.Options.IConfigureOptions<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>
{
    private readonly Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider _provider;

    /// <summary>
    /// Initializes a new instance of the ConfigureSwaggerOptions class.
    /// </summary>
    /// <param name="provider">API version description provider</param>
    public ConfigureSwaggerOptions(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Configures Swagger generation options for API versioning.
    /// </summary>
    /// <param name="options">Swagger generation options to configure</param>
    public void Configure(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        // Generate a Swagger document for each API version
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateVersionInfo(description));
        }
    }

    private static Microsoft.OpenApi.Models.OpenApiInfo CreateVersionInfo(Asp.Versioning.ApiExplorer.ApiVersionDescription description)
    {
        var info = new Microsoft.OpenApi.Models.OpenApiInfo()
        {
            Title = "ExpenseTracker API",
            Version = description.ApiVersion.ToString(),
            Description = "API for managing personal expenses and income tracking",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "ExpenseTracker Support",
                Email = "support@expensetracker.com"
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " - DEPRECATED";
        }

        return info;
    }
}
