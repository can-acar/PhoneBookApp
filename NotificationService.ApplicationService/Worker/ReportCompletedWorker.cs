using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.ApplicationService.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using System.Text;

namespace NotificationService.ApplicationService.Worker
{
    public class ReportCompletedWorker : BackgroundService
    {
        private readonly KafkaSettings _kafkaSettings;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReportCompletedWorker> _logger;
        private readonly ConsumerConfig _consumerConfig;

        public ReportCompletedWorker(
            IOptions<KafkaSettings> kafkaSettings,
            IServiceScopeFactory scopeFactory,
            ILogger<ReportCompletedWorker> logger)
        {
            _kafkaSettings = kafkaSettings.Value;
            _scopeFactory = scopeFactory;
            _logger = logger;

            _consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                GroupId = _kafkaSettings.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Kafka consumer for Report Completed events");

            await Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                    .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
                    .Build();

                consumer.Subscribe(_kafkaSettings.ReportCompletedTopic);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);

                            if (consumeResult == null)
                            {
                                continue;
                            }

                            _logger.LogInformation(
                                "Received report completed event: {Key}, Partition: {Partition}, Offset: {Offset}",
                                consumeResult.Message.Key, consumeResult.Partition, consumeResult.Offset);

                            // Process the message
                            await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                            // Commit the offset after processing
                            consumer.Commit(consumeResult);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, "Error consuming Kafka message");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // The token has been canceled
                    _logger.LogInformation("Report completed consumer is shutting down");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                }
                finally
                {
                    consumer.Close();
                }
            }, stoppingToken);
        }

        private async Task ProcessMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                var kafkaMessage = JsonConvert.DeserializeObject<KafkaMessage<ReportCompletedEvent>>(message);

                if (kafkaMessage?.Data == null)
                {
                    _logger.LogWarning("Invalid message format or empty data");
                    return;
                }

                var reportData = kafkaMessage.Data;

                _logger.LogInformation("Processing report completed for ReportId: {ReportId}, UserId: {UserId}",
                    reportData.ReportId, reportData.UserId);

                await SendNotificationsForCompletedReport(reportData, kafkaMessage.CorrelationId, cancellationToken);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error deserializing Kafka message: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Kafka message");
            }
        }

        private async Task SendNotificationsForCompletedReport(
            ReportCompletedEvent reportData,
            string correlationId,
            CancellationToken cancellationToken)
        {
            var notificationPrefs = reportData.NotificationPreferences ?? new NotificationPreferences();

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
                // Prepare template data
                var templateData = new Dictionary<string, object>
                {
                    { "ReportId", reportData.ReportId },
                    { "Location", reportData.Location },
                    { "CompletedAt", reportData.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "TotalPersons", reportData.Summary?.TotalPersons ?? 0 },
                    { "TotalPhoneNumbers", reportData.Summary?.TotalPhoneNumbers ?? 0 },
                    { "DownloadUrl", reportData.DownloadUrl },
                    { "ExpiryDate", reportData.DownloadExpiresAt.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                // Check if template exists and create it if it doesn't
                if (!await templateService.TemplateExistsAsync("ReportCompleted", notificationPrefs.Language))
                {
                    _logger.LogWarning(
                        "Template 'ReportCompleted' not found for language {Language}, falling back to default template",
                        notificationPrefs.Language);

                    // We could create a default template here in a real implementation
                }

                // Render the template
                (string subject, string content) = await templateService.RenderNotificationTemplateAsync(
                    "ReportCompleted",
                    templateData,
                    notificationPrefs.Language);

                // Send email notification if enabled
                if (notificationPrefs.EnableEmail && !string.IsNullOrEmpty(reportData.UserEmail))
                {
                    // Create notification with required parameters
                    var emailNotification = new Notification(
                        userId: reportData.UserId,
                        subject: subject,
                        content: content,
                        priority: NotificationPriority.Normal,
                        correlationId: correlationId);

                    // Set email address
                    emailNotification.SetRecipientEmail(reportData.UserEmail);

                    // Add additional data
                    emailNotification.AddAdditionalData("reportId", reportData.ReportId);

                    await notificationService.CreateNotificationAsync(emailNotification, cancellationToken);
                    await notificationService.SendNotificationAsync(emailNotification, cancellationToken);
                }

                // Send SMS notification if enabled
                if (notificationPrefs.EnableSms && !string.IsNullOrEmpty(reportData.UserPhoneNumber))
                {
                    var smsContent =
                        $"Rapor hazır: {reportData.Location} bölgesi için rapor tamamlandı. İndirme linki: {reportData.DownloadUrl}";

                    // Create notification with required parameters
                    var smsNotification = new Notification(
                        userId: reportData.UserId,
                        subject: "Rapor Hazır",
                        content: smsContent,
                        priority: NotificationPriority.Normal,
                        correlationId: correlationId);

                    // Set phone number
                    smsNotification.SetRecipientPhoneNumber(reportData.UserPhoneNumber);

                    // Set preferred provider to SMS
                    smsNotification.SetPreferredProvider(ProviderType.Sms);

                    // Add additional data
                    smsNotification.AddAdditionalData("reportId", reportData.ReportId);

                    await notificationService.CreateNotificationAsync(smsNotification, cancellationToken);
                    await notificationService.SendNotificationAsync(smsNotification, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for completed report {ReportId}", reportData.ReportId);
            }
        }
    }
}