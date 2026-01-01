using SampleCkWebApp.Application;
using SampleCkWebApp.WebApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
{
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory);
    
    // Support DATABASE_URL environment variable (common in cloud platforms)
    // This must be done BEFORE any configuration is read
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        // Convert DATABASE_URL format (postgresql://user:pass@host:port/db?params)
        // to Npgsql connection string format
        var connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
        builder.Configuration["Database:ConnectionString"] = connectionString;
    }
    else
    {
        // Also check if it's set in configuration (for appsettings.json)
        var configConnectionString = builder.Configuration["Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(configConnectionString))
        {
            // If neither is set, throw a clear error
            throw new InvalidOperationException(
                "Database connection string is required. Set DATABASE_URL environment variable or Database:ConnectionString in appsettings.json");
        }
    }

    // Create the logger
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Filter.ByExcluding(logEvent => 
            logEvent.Properties.TryGetValue("RequestPath", out var property)
            && property.ToString().StartsWith("\"/health"))
        .CreateLogger();
    
    // Add logger to logging pipeline
    builder.Logging
        .ClearProviders()
        .AddSerilog(Log.Logger);
    
    builder.Host.UseSerilog();
    
    // CORS: allow the Vite frontend to call this API during development
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin());
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        // Define API versions
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Expense Tracker API",
            Version = "v1",
            Description = "A RESTful API for managing expenses, users, and categories in an expense tracking application.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "API Support",
                Email = "support@expensetracker.com"
            }
        });
        
        c.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Expense Tracker API",
            Version = "v2",
            Description = "Version 2 of the Expense Tracker API with new features.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "API Support",
                Email = "support@expensetracker.com"
            }
        });
        
        // Route endpoints to correct doc based on path
        c.DocInclusionPredicate((docName, apiDesc) =>
        {
            var path = apiDesc.RelativePath ?? "";
            return docName switch
            {
                "v1" => path.StartsWith("api/v1") || path == "health",
                "v2" => path.StartsWith("api/v2"),
                _ => false
            };
        });
        
        // Include XML comments
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
        
        // Include XML comments from referenced projects
        var contractsXml = Path.Combine(AppContext.BaseDirectory, "SampleCkWebApp.Contracts.xml");
        if (File.Exists(contractsXml))
        {
            c.IncludeXmlComments(contractsXml);
        }
        
        // Use full names for better organization
        c.CustomSchemaIds(type => type.FullName);
    });    
    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration);
}

Log.Logger.Information("Application starting");

var app = builder.Build();
{
    // Save service provider to static class
    ServicePool.Create(app.Services);

    // Add exception handler and request logging
    app.UseExceptionHandler("/error");
    
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.ConfigureAuditLogging(httpContext);
        };
    });
    
    // CORS must be early in the pipeline to handle preflight OPTIONS requests
    app.UseCors();
    
    // Swagger should be before UseRouting to avoid authorization issues
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Add endpoints for each version (newest first)
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Expense Tracker API v2");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Expense Tracker API v1");
        c.RoutePrefix = "swagger"; // Swagger UI available at /swagger
        c.DocumentTitle = "Expense Tracker API Documentation";
        c.DefaultModelsExpandDepth(-1); // Hide schemas by default
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
    });
    
    app.UseRouting();
    
    app.MapControllers();
}

// Start the application
app.Start();

Log.Logger.Information("Application started");
foreach (var url in app.Urls)
{
    Log.Logger.Information("Listening on: {url}", url);
}

app.WaitForShutdown();

Log.Logger.Information("Application shutdown gracefully");

// Converts DATABASE_URL format (postgresql://user:pass@host:port/db?params)
// to Npgsql connection string format (Host=host;Port=port;Database=db;Username=user;Password=pass;params)
static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    try
    {
        var uri = new Uri(databaseUrl);
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');
        var username = uri.UserInfo.Split(':')[0];
        var password = uri.UserInfo.Split(':').Length > 1 ? uri.UserInfo.Split(':')[1] : "";
        
        var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={Uri.UnescapeDataString(password)}";
        
        // Add query string parameters (like sslmode)
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var query = uri.Query.TrimStart('?');
            var pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    var key = Uri.UnescapeDataString(parts[0]);
                    var value = Uri.UnescapeDataString(parts[1]);
                    connectionString += $";{key}={value}";
                }
            }
        }
        
        return connectionString;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Failed to parse DATABASE_URL: {ex.Message}", ex);
    }
}
