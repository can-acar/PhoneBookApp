namespace NotificationService.ApiContract.Contracts.Models;

public class ContactsByLocationResponse
{
    public List<ContactGraphQLSummary>? GetContactsByLocation { get; set; }
}