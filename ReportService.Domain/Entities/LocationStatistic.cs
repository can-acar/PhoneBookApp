namespace ReportService.Domain.Entities;

public class LocationStatistic
{
    // Private constructor for infrastructure (ORM/Serialization)
    private LocationStatistic() { }

    // Rich constructor with business rules
    public LocationStatistic(string location, int personCount, int phoneNumberCount)
    {
        ValidateLocation(location);
        ValidatePersonCount(personCount);
        ValidatePhoneNumberCount(phoneNumberCount);

        Id = Guid.NewGuid();
        Location = location.Trim();
        PersonCount = personCount;
        PhoneNumberCount = phoneNumberCount;
    }

    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public int PersonCount { get; private set; }
    public int PhoneNumberCount { get; private set; }
    
    // Navigation property
    public virtual Report Report { get; private set; } = null!;

    // Computed properties
    public double AveragePhoneNumbersPerPerson => PersonCount > 0 ? (double)PhoneNumberCount / PersonCount : 0;
    public bool HasMultiplePhoneNumbers => PhoneNumberCount > PersonCount;

    // Business methods
    public void UpdateCounts(int personCount, int phoneNumberCount)
    {
        ValidatePersonCount(personCount);
        ValidatePhoneNumberCount(phoneNumberCount);

        PersonCount = personCount;
        PhoneNumberCount = phoneNumberCount;
    }

    public void IncrementPersonCount(int increment = 1)
    {
        if (increment <= 0)
        {
            throw new ArgumentException("Increment must be positive", nameof(increment));
        }

        PersonCount += increment;
    }

    public void IncrementPhoneNumberCount(int increment = 1)
    {
        if (increment <= 0)
        {
            throw new ArgumentException("Increment must be positive", nameof(increment));
        }

        PhoneNumberCount += increment;
    }

    public void SetReportId(Guid reportId)
    {
        if (reportId == Guid.Empty)
        {
            throw new ArgumentException("Report ID cannot be empty", nameof(reportId));
        }

        ReportId = reportId;
    }

    // Private validation methods
    private static void ValidateLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new ArgumentException("Location cannot be null or empty", nameof(location));
        }

        if (location.Trim().Length > 200)
        {
            throw new ArgumentException("Location cannot exceed 200 characters", nameof(location));
        }
    }

    private static void ValidatePersonCount(int personCount)
    {
        if (personCount < 0)
        {
            throw new ArgumentException("Person count cannot be negative", nameof(personCount));
        }
    }

    private static void ValidatePhoneNumberCount(int phoneNumberCount)
    {
        if (phoneNumberCount < 0)
        {
            throw new ArgumentException("Phone number count cannot be negative", nameof(phoneNumberCount));
        }
    }
}