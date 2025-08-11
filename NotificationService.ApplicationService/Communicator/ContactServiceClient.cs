using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using NotificationService.ApiContract.Contracts.External;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;

namespace NotificationService.ApplicationService.Communicator;

public class ContactServiceClient : IContactServiceClient
{
    private readonly GraphQLHttpClient _client;
    private readonly ILogger<ContactServiceClient> _logger;

    public ContactServiceClient(string contactServiceUrl, ILogger<ContactServiceClient> logger)
    {
        _logger = logger;
        _client = new GraphQLHttpClient($"{contactServiceUrl}/graphql", new NewtonsoftJsonSerializer());
    }

    public async Task<IEnumerable<ContactSummary>> GetContactsByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GraphQLRequest
            {
                Query = @"
                query GetContactsByLocation($location: String!) {
                    getContactsByLocation(location: $location) {
                        id
                        name
                        company
                    }
                }",
                Variables = new { location }
            };

            var response = await _client.SendQueryAsync<NotificationContactsByLocationResponse>(query, cancellationToken);

            if (response.Errors?.Any() == true)
            {
                _logger.LogError("GraphQL errors: {Errors}", string.Join(", ", response.Errors.Select(e => e.Message)));
                return new List<ContactSummary>();
            }

            return response.Data?.GetContactsByLocation?.Select(c => new ContactSummary
            {
                Id = c.Id,
                Name = c.Name,
                Company = c.Company,
                ContactInfos = new()
            }) ?? new List<ContactSummary>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts by location: {Location}", location);
            return new List<ContactSummary>();
        }
    }

    public async Task<IEnumerable<ContactSummary>> GetAllContactsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GraphQLRequest
            {
                Query = @"
                query GetAllContacts($page: Int!, $pageSize: Int!) {
                    getContacts(page: $page, pageSize: $pageSize) {
                        id
                        name
                        company
                    }
                }",
                Variables = new { page = 1, pageSize = 1000 }
            };

            var response = await _client.SendQueryAsync<NotificationAllContactsResponse>(query, cancellationToken);

            if (response.Errors?.Any() == true)
            {
                _logger.LogError("GraphQL errors: {Errors}", string.Join(", ", response.Errors.Select(e => e.Message)));
                return new List<ContactSummary>();
            }

            return response.Data?.GetContacts?.Select(c => new ContactSummary
            {
                Id = c.Id,
                Name = c.Name,
                Company = c.Company,
                ContactInfos = new()
            }) ?? new List<ContactSummary>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all contacts");
            return new List<ContactSummary>();
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}