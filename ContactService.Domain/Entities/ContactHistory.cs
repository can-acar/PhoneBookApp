using System.Text.Json;

namespace ContactService.Domain.Entities
{
    public class ContactHistory
    {
        public Guid Id { get; private set; }
        public Guid ContactId { get; private set; }
        public string OperationType { get; private set; } = string.Empty; // CREATE, UPDATE, DELETE
        public string Data { get; private set; } = string.Empty; // JSON snapshot of contact data
        public DateTime Timestamp { get; private set; }
        public string CorrelationId { get; private set; } = string.Empty;
        public string? UserId { get; private set; }
        public string? IPAddress { get; private set; }
        public string? UserAgent { get; private set; }
        public string? AdditionalMetadata { get; private set; }

        // Private constructor for EF Core
        private ContactHistory() { }

        public ContactHistory(
            Guid contactId,
            string operationType,
            object contactData,
            string correlationId,
            string? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            Dictionary<string, object>? additionalMetadata = null)
        {
            ValidateContactId(contactId);
            ValidateOperationType(operationType);
            ValidateContactData(contactData);
            ValidateCorrelationId(correlationId);

            Id = Guid.NewGuid();
            ContactId = contactId;
            OperationType = operationType.Trim().ToUpperInvariant();
            Data = JsonSerializer.Serialize(contactData, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            Timestamp = DateTime.UtcNow;
            CorrelationId = correlationId.Trim();
            UserId = userId?.Trim();
            IPAddress = ipAddress?.Trim();
            UserAgent = userAgent?.Trim();

            if (additionalMetadata != null && additionalMetadata.Any())
            {
                AdditionalMetadata = JsonSerializer.Serialize(additionalMetadata, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
        }

        public T? GetContactData<T>() where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(Data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        public Dictionary<string, object>? GetAdditionalMetadata()
        {
            if (string.IsNullOrWhiteSpace(AdditionalMetadata))
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalMetadata, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private static void ValidateContactId(Guid contactId)
        {
            if (contactId == Guid.Empty)
                throw new ArgumentException("Contact ID cannot be empty", nameof(contactId));
        }

        private static void ValidateOperationType(string operationType)
        {
            if (string.IsNullOrWhiteSpace(operationType))
                throw new ArgumentException("Operation type cannot be null or empty", nameof(operationType));

            var validOperations = new[] { "CREATE", "UPDATE", "DELETE" };
            if (!validOperations.Contains(operationType.Trim().ToUpperInvariant()))
                throw new ArgumentException($"Operation type must be one of: {string.Join(", ", validOperations)}", nameof(operationType));
        }

        private static void ValidateContactData(object contactData)
        {
            if (contactData == null)
                throw new ArgumentException("Contact data cannot be null", nameof(contactData));
        }

        private static void ValidateCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));

            if (correlationId.Trim().Length > 100)
                throw new ArgumentException("Correlation ID cannot exceed 100 characters", nameof(correlationId));
        }
    }

    public static class ContactHistoryOperationType
    {
        public const string CREATE = "CREATE";
        public const string UPDATE = "UPDATE";
        public const string DELETE = "DELETE";
    }
}