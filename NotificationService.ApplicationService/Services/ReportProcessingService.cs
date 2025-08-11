using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReportService.Domain.Interfaces;

namespace NotificationService.ApplicationService.Services
{
    public class ReportProcessingService : BackgroundService
    {
        private readonly IKafkaConsumer _kafkaConsumer;
        private readonly ILogger<ReportProcessingService> _logger;

        public ReportProcessingService(IKafkaConsumer kafkaConsumer, ILogger<ReportProcessingService> logger)
        {
            _kafkaConsumer = kafkaConsumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Report processing service is starting.");
            
                _kafkaConsumer.StartConsuming(stoppingToken);
            
                // Keep the service running
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Report processing service is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the report processing service.");
            }
            finally
            {
                _kafkaConsumer.StopConsuming();
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Report processing service is stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}
