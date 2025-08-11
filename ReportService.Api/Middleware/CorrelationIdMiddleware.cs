namespace ReportService.Api.Middleware;

/// <summary>
/// ReportService-specific correlation ID middleware wrapper
/// </summary>
public class CorrelationIdMiddleware : Shared.CrossCutting.Middleware.CorrelationIdMiddleware
{
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        : base(next, logger)
    {
        // Set service name for ReportService
        Environment.SetEnvironmentVariable("SERVICE_NAME", "ReportService");
    }
}