namespace ContactService.Domain.Interfaces;

public interface IKafkaProducer
{
    Task PublishAsync(string topic, string message, string? correlationId = null, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string topic, T data, string? correlationId = null, CancellationToken cancellationToken = default) where T : class;
}