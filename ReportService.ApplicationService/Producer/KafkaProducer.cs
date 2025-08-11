using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Models.Kafka;
using ReportService.Infrastructure.Configuration;

namespace ReportService.ApplicationService.Producer;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly KafkaSettings _settings;

    public KafkaProducer(KafkaSettings settings, ILogger<KafkaProducer> logger)
    {
        _settings = settings;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            ClientId = settings.ClientId,
            // Add more configuration as needed
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishReportRequestAsync(ReportRequestMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            string messageJson = JsonSerializer.Serialize(message);
            string messageKey = message.ReportId.ToString();

            var headers = new Headers();
            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                headers.Add("X-Correlation-ID", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId));
            }

            var kafkaMessage = new Message<string, string>
            {
                Key = messageKey,
                Value = messageJson,
                Headers = headers
            };

            var deliveryResult = await _producer.ProduceAsync(
                _settings.Topics.ReportRequests, 
                kafkaMessage, 
                cancellationToken);

            _logger.LogInformation("Published report request message {ReportId} to topic {Topic} at {Timestamp}",
                message.ReportId, _settings.Topics.ReportRequests, deliveryResult.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing report request message for report {ReportId}", message.ReportId);
            throw;
        }
    }

    public async Task PublishReportCompletedAsync(ReportCompletedMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            string messageJson = JsonSerializer.Serialize(message);
            string messageKey = message.ReportId.ToString();

            var headers = new Headers();
            if (!string.IsNullOrEmpty(message.CorrelationId))
            {
                headers.Add("X-Correlation-ID", System.Text.Encoding.UTF8.GetBytes(message.CorrelationId));
            }

            var kafkaMessage = new Message<string, string>
            {
                Key = messageKey,
                Value = messageJson,
                Headers = headers
            };

            var deliveryResult = await _producer.ProduceAsync(
                _settings.Topics.ReportCompleted, 
                kafkaMessage, 
                cancellationToken);

            _logger.LogInformation("Published report completed message {ReportId} to topic {Topic} at {Timestamp}",
                message.ReportId, _settings.Topics.ReportCompleted, deliveryResult.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing report completed message for report {ReportId}", message.ReportId);
            throw;
        }
    }
}
