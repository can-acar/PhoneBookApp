using System.Text.RegularExpressions;
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities
{
    public class Notification
    {
        private readonly Dictionary<string, string> _additionalData = new();

        // Private constructor for infrastructure (ORM/Serialization)
        private Notification() { }

        // Rich constructor with business rules
        public Notification(string userId, string subject, string content, NotificationPriority priority, string correlationId)
        {
            ValidateUserId(userId);
            ValidateSubject(subject);
            ValidateContent(content);
            ValidateCorrelationId(correlationId);

            Id = Guid.NewGuid();
            UserId = userId.Trim();
            Subject = subject.Trim();
            Content = content.Trim();
            Priority = priority;
            CorrelationId = correlationId.Trim();
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public string UserId { get; private set; } = string.Empty;
        public string Subject { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public string? RecipientEmail { get; private set; }
        public string? RecipientPhoneNumber { get; private set; }
        public NotificationPriority Priority { get; private set; }
        public ProviderType PreferredProvider { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? SentAt { get; private set; }
        public bool IsDelivered { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string CorrelationId { get; private set; } = string.Empty;

        // Encapsulated collection
        public virtual Dictionary<string, string> AdditionalData 
        { 
            get => _additionalData; 
            private set 
            {
                _additionalData.Clear();
                if (value != null)
                {
                    foreach (var kvp in value)
                        _additionalData.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public IReadOnlyDictionary<string, string> AdditionalDataReadOnly => _additionalData.AsReadOnly();

        // Computed properties
        public bool HasRecipientEmail => !string.IsNullOrWhiteSpace(RecipientEmail);
        public bool HasRecipientPhoneNumber => !string.IsNullOrWhiteSpace(RecipientPhoneNumber);
        public bool HasBeenSent => SentAt.HasValue;
        public bool IsHighPriority => Priority == NotificationPriority.High || Priority == NotificationPriority.Critical;
        public bool HasFailed => !string.IsNullOrWhiteSpace(ErrorMessage);

        // Business methods
        public void SetRecipientEmail(string email)
        {
            ValidateEmail(email);
            RecipientEmail = email.Trim().ToLowerInvariant();
            
            if (PreferredProvider == ProviderType.Unknown)
            {
                PreferredProvider = ProviderType.Email;
            }
        }

        public void SetRecipientPhoneNumber(string phoneNumber)
        {
            ValidatePhoneNumber(phoneNumber);
            RecipientPhoneNumber = NormalizePhoneNumber(phoneNumber);
            
            if (PreferredProvider == ProviderType.Unknown)
            {
                PreferredProvider = ProviderType.Sms;
            }
        }

        public void SetPreferredProvider(ProviderType providerType)
        {
            if (providerType == ProviderType.Unknown)
            {
                throw new ArgumentException("Preferred provider cannot be Unknown", nameof(providerType));
            }

            // Validate that we have the necessary recipient information
            switch (providerType)
            {
                case ProviderType.Email when string.IsNullOrWhiteSpace(RecipientEmail):
                    throw new InvalidOperationException("Cannot set Email provider without recipient email");
                case ProviderType.Sms when string.IsNullOrWhiteSpace(RecipientPhoneNumber):
                    throw new InvalidOperationException("Cannot set SMS provider without recipient phone number");
            }

            PreferredProvider = providerType;
        }

        public void MarkAsSent(ProviderType sentViaProvider)
        {
            if (HasBeenSent)
            {
                throw new InvalidOperationException("Notification has already been sent");
            }

            if (sentViaProvider == ProviderType.Unknown)
            {
                throw new ArgumentException("Sent via provider cannot be Unknown", nameof(sentViaProvider));
            }

            SentAt = DateTime.UtcNow;
            IsDelivered = true;
            ErrorMessage = null; // Clear any previous error
        }

        public void MarkAsFailed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be empty", nameof(errorMessage));
            }

            if (errorMessage.Length > 1000)
            {
                throw new ArgumentException("Error message cannot exceed 1000 characters", nameof(errorMessage));
            }

            ErrorMessage = errorMessage.Trim();
            IsDelivered = false;
            SentAt = null;
        }

        public void AddAdditionalData(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or empty", nameof(value));
            }

            if (key.Length > 100)
            {
                throw new ArgumentException("Key cannot exceed 100 characters", nameof(key));
            }

            if (value.Length > 500)
            {
                throw new ArgumentException("Value cannot exceed 500 characters", nameof(value));
            }

            _additionalData[key.Trim()] = value.Trim();
        }

        public void RemoveAdditionalData(string key)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                _additionalData.Remove(key.Trim());
            }
        }

        public void UpdatePriority(NotificationPriority newPriority)
        {
            if (HasBeenSent)
            {
                throw new InvalidOperationException("Cannot update priority of sent notification");
            }

            Priority = newPriority;
        }

        public void UpdateContent(string newSubject, string newContent)
        {
            if (HasBeenSent)
            {
                throw new InvalidOperationException("Cannot update content of sent notification");
            }

            ValidateSubject(newSubject);
            ValidateContent(newContent);

            Subject = newSubject.Trim();
            Content = newContent.Trim();
        }

        public bool HasValidRecipient()
        {
            return HasRecipientEmail || HasRecipientPhoneNumber;
        }

        // Private validation methods
        private static void ValidateUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            if (userId.Trim().Length > 100)
            {
                throw new ArgumentException("User ID cannot exceed 100 characters", nameof(userId));
            }
        }

        private static void ValidateSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentException("Subject cannot be null or empty", nameof(subject));
            }

            if (subject.Trim().Length > 200)
            {
                throw new ArgumentException("Subject cannot exceed 200 characters", nameof(subject));
            }
        }

        private static void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Content cannot be null or empty", nameof(content));
            }

            if (content.Trim().Length > 2000)
            {
                throw new ArgumentException("Content cannot exceed 2000 characters", nameof(content));
            }
        }

        private static void ValidateCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));
            }

            if (correlationId.Trim().Length > 100)
            {
                throw new ArgumentException("Correlation ID cannot exceed 100 characters", nameof(correlationId));
            }
        }

        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }

            if (email.Length > 254)
            {
                throw new ArgumentException("Email cannot exceed 254 characters", nameof(email));
            }

            // Simple email validation regex
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            if (!emailRegex.IsMatch(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
        }

        private static void ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be null or empty", nameof(phoneNumber));
            }

            // Remove all non-digit characters for validation
            var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");
            
            if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
            {
                throw new ArgumentException("Phone number must contain 10-15 digits", nameof(phoneNumber));
            }
        }

        private static string NormalizePhoneNumber(string phoneNumber)
        {
            // Keep only digits and common formatting characters
            return Regex.Replace(phoneNumber.Trim(), @"[^\d\+\-\s\(\)]", "");
        }
    }
}
