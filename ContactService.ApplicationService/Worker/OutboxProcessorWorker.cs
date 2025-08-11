using ContactService.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContactService.ApplicationService.Worker;

public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorWorker> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private DateTime _lastCleanup = DateTime.UtcNow;

    public OutboxProcessorWorker(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var scopeFactory = (IServiceScopeFactory)_serviceProvider.GetService(typeof(IServiceScopeFactory))!;
                using var scope = scopeFactory.CreateScope();
                var outboxService = (IOutboxService)scope.ServiceProvider.GetService(typeof(IOutboxService))!;

                // Process pending events
                await ProcessPendingEventsAsync(outboxService, stoppingToken);

                // Process failed events (retry)
                await ProcessFailedEventsAsync(outboxService, stoppingToken);

                // Periodic cleanup
                await PerformPeriodicCleanupAsync(outboxService, stoppingToken);

                // Wait before next processing cycle
                await Task.Delay(_processingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Outbox Processor Worker is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Outbox Processor Worker");
                
                // Wait before retrying to avoid tight loop on persistent errors
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Outbox Processor Worker stopped");
    }

    private async Task ProcessPendingEventsAsync(IOutboxService outboxService, CancellationToken cancellationToken)
    {
        try
        {
            await outboxService.ProcessPendingEventsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending outbox events");
        }
    }

    private async Task ProcessFailedEventsAsync(IOutboxService outboxService, CancellationToken cancellationToken)
    {
        try
        {
            await outboxService.ProcessFailedEventsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed outbox events");
        }
    }

    private async Task PerformPeriodicCleanupAsync(IOutboxService outboxService, CancellationToken cancellationToken)
    {
        try
        {
            if (DateTime.UtcNow - _lastCleanup > _cleanupInterval)
            {
                await outboxService.CleanupProcessedEventsAsync(7, cancellationToken);
                _lastCleanup = DateTime.UtcNow;
                
                _logger.LogInformation("Performed periodic cleanup of processed outbox events");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during periodic cleanup of outbox events");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox Processor Worker is stopping...");

        try
        {
            // Process any remaining pending events before shutdown
            var scopeFactory = (IServiceScopeFactory)_serviceProvider.GetService(typeof(IServiceScopeFactory))!;
            using var scope = scopeFactory.CreateScope();
            var outboxService = (IOutboxService)scope.ServiceProvider.GetService(typeof(IOutboxService))!;
            
            await outboxService.ProcessPendingEventsAsync(cancellationToken);
            _logger.LogInformation("Processed remaining outbox events before shutdown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing remaining outbox events during shutdown");
        }

        await base.StopAsync(cancellationToken);
    }
}
