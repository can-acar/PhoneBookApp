namespace NotificationService.ApiContract.Contracts.Models;

public class AllContactsResponse
{
    public List<ContactGraphQLSummary>? GetContacts { get; set; }
}