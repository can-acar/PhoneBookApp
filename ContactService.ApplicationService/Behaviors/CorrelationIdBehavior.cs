using ContactService.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContactService.ApplicationService.Behaviors;

/// <summary>
/// Pipeline behavior to ensure correlation ID is available in all MediatR requests
/// </summary>
public class CorrelationIdBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ICorrelationContext _correlationContext;
    private readonly ILogger<CorrelationIdBehavior<TRequest, TResponse>> _logger;

    public CorrelationIdBehavior(
        ICorrelationContext correlationContext,
        ILogger<CorrelationIdBehavior<TRequest, TResponse>> logger)
    {
        _correlationContext = correlationContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var correlationId = _correlationContext.CorrelationId;
        var requestName = typeof(TRequest).Name;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestType"] = requestName
        }))
        {
            _logger.LogDebug("Handling {RequestType} with correlation ID: {CorrelationId}", 
                requestName, correlationId);

            try
            {
                var response = await next();
                
                _logger.LogDebug("Successfully handled {RequestType} with correlation ID: {CorrelationId}", 
                    requestName, correlationId);
                    
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling {RequestType} with correlation ID: {CorrelationId}", 
                    requestName, correlationId);
                throw;
            }
        }
    }
}