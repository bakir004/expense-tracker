using SampleCkWebApp.Application;
using SampleCkWebApp.WebApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
{
    Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory);

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

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
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
        
        // Add server with base path so Swagger knows the prefix
        c.AddServer(new Microsoft.OpenApi.Models.OpenApiServer
        {
            Url = "/api/v1",
            Description = "API v1"
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
    
    // Global API version prefix - all controller routes will be prefixed with /api/v1
    app.UsePathBase("/api/v1");
    
    // Swagger should be before UseRouting to avoid authorization issues
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
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
