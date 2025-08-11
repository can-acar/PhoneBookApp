namespace NotificationService.ApiContract.Contracts.Models;

public class ContactGraphQLSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}