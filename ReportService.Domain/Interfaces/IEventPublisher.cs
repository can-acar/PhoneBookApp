namespace ReportService.Domain.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T eventData, CancellationToken cancellationToken = default);
}
