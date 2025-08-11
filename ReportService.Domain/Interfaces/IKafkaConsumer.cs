using ReportService.Domain.Models.Kafka;

namespace ReportService.Domain.Interfaces
{
    /// <summary>
    /// Kafka consumer interface for clean architecture
    /// Domain layer: Defines contract for message consumption
    /// </summary>
    public interface IKafkaConsumer
    {
        /// <summary>
        /// Start consuming messages from specified topic
        /// </summary>
        /// <param name="topic">Kafka topic name</param>
        /// <param name="messageHandler">Handler function for processing messages</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ConsumeAsync(string topic, Func<string, CancellationToken, Task> messageHandler, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        void StartConsuming(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop consuming messages
        /// </summary>
        void StopConsuming();
    }
}
