using Microsoft.AspNetCore.Http;
using Serilog;

namespace SampleCkWebApp.WebApi;

public static class DiagnosticContextExtensions
{
    public static void ConfigureAuditLogging(this IDiagnosticContext diagnosticContext, HttpContext httpContext)
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("ContentLength", httpContext.Request.ContentLength?.ToString());
    }
}