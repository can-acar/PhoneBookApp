using ReportService.Domain.Enums;

namespace ReportService.Domain.Entities;

public class Report
{
    private readonly List<LocationStatistic> _locationStatistics = new();

    // Private constructor for infrastructure (ORM/Serialization)
    private Report() { }

    // Rich constructor with business rules
    public Report(string location, string requestedBy)
    {
        ValidateLocation(location);
        ValidateRequestedBy(requestedBy);

        Id = Guid.NewGuid();
        Location = string.IsNullOrWhiteSpace(location) ? string.Empty : location.Trim();
        RequestedBy = requestedBy.Trim();
        Status = ReportStatus.Preparing;
        RequestedAt = DateTime.UtcNow;
        FileFormat = "JSON";
    }

    public Guid Id { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public ReportStatus Status { get; private set; } = ReportStatus.Preparing;
    public DateTime RequestedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    
    // Report Results
    public int TotalPersonCount { get; private set; }
    public int TotalPhoneNumberCount { get; private set; }
    
    // Metadata
    public string RequestedBy { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    
    // File information
    public string? FilePath { get; private set; }
    public string? FileFormat { get; private set; } = "JSON";
    public long? FileSizeBytes { get; private set; }
    
    // Encapsulated collection
    public virtual ICollection<LocationStatistic> LocationStatistics 
    { 
        get => _locationStatistics; 
        private set => _locationStatistics.AddRange(value); 
    }
    
    public IReadOnlyCollection<LocationStatistic> LocationStatisticsReadOnly => _locationStatistics.AsReadOnly();
    
    // Computed properties
    public TimeSpan? ProcessingDuration => CompletedAt?.Subtract(RequestedAt);
    public bool IsCompleted => Status == ReportStatus.Completed;
    public bool HasFailed => Status == ReportStatus.Failed;
    public bool IsInProgress => Status == ReportStatus.InProgress;

    // Business methods
    public void MarkAsInProgress()
    {
        if (Status != ReportStatus.Preparing)
        {
            throw new InvalidOperationException($"Cannot mark report as in progress from status: {Status}");
        }

        Status = ReportStatus.InProgress;
    }

    public void MarkAsCompleted(int totalPersonCount, int totalPhoneNumberCount, string? filePath = null, long? fileSizeBytes = null)
    {
        if (Status != ReportStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete report from status: {Status}");
        }

        if (totalPersonCount < 0)
        {
            throw new ArgumentException("Total person count cannot be negative", nameof(totalPersonCount));
        }

        if (totalPhoneNumberCount < 0)
        {
            throw new ArgumentException("Total phone number count cannot be negative", nameof(totalPhoneNumberCount));
        }

        Status = ReportStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        TotalPersonCount = totalPersonCount;
        TotalPhoneNumberCount = totalPhoneNumberCount;
        FilePath = filePath;
        FileSizeBytes = fileSizeBytes;
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

        Status = ReportStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage.Trim();
    }

    public void AddLocationStatistic(string location, int personCount, int phoneNumberCount)
    {
        ValidateLocation(location);

        if (personCount < 0)
        {
            throw new ArgumentException("Person count cannot be negative", nameof(personCount));
        }

        if (phoneNumberCount < 0)
        {
            throw new ArgumentException("Phone number count cannot be negative", nameof(phoneNumberCount));
        }

        if (HasLocationStatistic(location))
        {
            throw new InvalidOperationException($"Location statistic for '{location}' already exists");
        }

        var locationStatistic = new LocationStatistic(location, personCount, phoneNumberCount);
        _locationStatistics.Add(locationStatistic);
    }

    public bool HasLocationStatistic(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return false;

        return _locationStatistics.Any(ls => string.Equals(ls.Location, location.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public LocationStatistic? GetLocationStatistic(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return null;

        return _locationStatistics.FirstOrDefault(ls => 
            string.Equals(ls.Location, location.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public void UpdateFileInformation(string filePath, string fileFormat, long fileSizeBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty", nameof(filePath));
        }

        if (string.IsNullOrWhiteSpace(fileFormat))
        {
            throw new ArgumentException("File format cannot be empty", nameof(fileFormat));
        }

        if (fileSizeBytes < 0)
        {
            throw new ArgumentException("File size cannot be negative", nameof(fileSizeBytes));
        }

        FilePath = filePath.Trim();
        FileFormat = fileFormat.Trim();
        FileSizeBytes = fileSizeBytes;
    }

    // Private validation methods
    private static void ValidateLocation(string location)
    {
        // Allow empty/null locations for reports with empty location
        if (location == null)
        {
            return; // null is acceptable, will be converted to empty string
        }

        if (location.Trim().Length > 200)
        {
            throw new ArgumentException("Location cannot exceed 200 characters", nameof(location));
        }
    }

    private static void ValidateRequestedBy(string requestedBy)
    {
        if (string.IsNullOrWhiteSpace(requestedBy))
        {
            throw new ArgumentException("RequestedBy cannot be null or empty", nameof(requestedBy));
        }

        if (requestedBy.Trim().Length > 100)
        {
            throw new ArgumentException("RequestedBy cannot exceed 100 characters", nameof(requestedBy));
        }
    }
}