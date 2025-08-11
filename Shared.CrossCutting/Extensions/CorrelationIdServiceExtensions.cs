using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Shared.CrossCutting.CorrelationId;
using Shared.CrossCutting.Middleware;

namespace Shared.CrossCutting.Extensions;

/// <summary>
/// Service collection extensions for correlation ID setup
/// </summary>
public static class CorrelationIdServiceExtensions
{
    /// <summary>
    /// Adds correlation ID provider and related services
    /// </summary>
    public static IServiceCollection AddCorrelationId(this IServiceCollection services)
    {
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();

        return services;
    }
}