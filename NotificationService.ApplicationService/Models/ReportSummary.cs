namespace NotificationService.ApplicationService.Models;

public class ReportSummary
{
    public int TotalPersons { get; set; }
    public int TotalPhoneNumbers { get; set; }
    public decimal AverageContactsPerPerson { get; set; }
}