using ReportService.Domain.Models.Kafka;

namespace ReportService.Domain.Interfaces
{
    public interface IKafkaProducer
    {
        Task PublishReportRequestAsync(ReportRequestMessage message, CancellationToken cancellationToken = default);
        Task PublishReportCompletedAsync(ReportCompletedMessage message, CancellationToken cancellationToken = default);
    }
}
