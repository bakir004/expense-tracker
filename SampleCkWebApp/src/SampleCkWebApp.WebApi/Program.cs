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
    
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "API", Version = "v1" });
    });    
    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration);
}

Log.Logger.Information("Application starting");

var app = builder.Build();
{
    app.UseRouting();
    
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
    
    app.UseSwagger();
    app.UseSwaggerUI();
    
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
