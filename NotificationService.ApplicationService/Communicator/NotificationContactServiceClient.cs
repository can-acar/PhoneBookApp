using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using NotificationService.ApiContract.Contracts.Models;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Interfaces;
using Shared.CrossCutting.CorrelationId;

namespace NotificationService.ApplicationService.Communicator;

public class NotificationContactServiceClient : IContactServiceClient
{
    private readonly GraphQLHttpClient _client;
    private readonly ILogger<NotificationContactServiceClient> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public NotificationContactServiceClient(
        string contactServiceUrl, 
        ILogger<NotificationContactServiceClient> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
        
        // Ensure URL is properly formatted
        if (!contactServiceUrl.EndsWith("/"))
            contactServiceUrl = contactServiceUrl + "/";
            
        _logger.LogInformation("Initializing NotificationContactServiceClient with URL: {Url}", contactServiceUrl);
        
        // Configure client to bypass SSL validation in development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            var httpClient = new HttpClient(handler);
            _client = new GraphQLHttpClient(
                new GraphQLHttpClientOptions { EndPoint = new Uri($"{contactServiceUrl}graphql") }, 
                new NewtonsoftJsonSerializer(), 
                httpClient);
        }
        else
        {
            _client = new GraphQLHttpClient($"{contactServiceUrl}graphql", new NewtonsoftJsonSerializer());
        }
        
        // Configure client
        _client.HttpClient.DefaultRequestHeaders.Add("User-Agent", "NotificationService/1.0");
    }

    public async Task<IEnumerable<ContactSummary>> GetContactsByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;
        
        using (_logger.BeginScope(new Dictionary<string, object> 
        { 
            ["CorrelationId"] = correlationId,
            ["Operation"] = "GetContactsByLocation",
            ["Location"] = location
        }))
        {
            try
            {
                _logger.LogInformation("Getting contacts by location: {Location} with correlation ID: {CorrelationId}", 
                    location, correlationId);

                // Add correlation ID to request headers
                _client.HttpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
                _client.HttpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

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

                var response = await _client.SendQueryAsync<ContactsByLocationResponse>(query, cancellationToken);

                if (response.Errors?.Any() == true)
                {
                    var errors = string.Join(", ", response.Errors.Select(e => e.Message));
                    _logger.LogError("GraphQL errors for correlation ID {CorrelationId}: {Errors}", 
                        correlationId, errors);
                    return new List<ContactSummary>();
                }

                var contacts = response.Data?.GetContactsByLocation?.Select(c => new ContactSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    Company = c.Company,
                    ContactInfos = new()
                }) ?? new List<ContactSummary>();

                _logger.LogInformation("Retrieved {Count} contacts for location: {Location} with correlation ID: {CorrelationId}", 
                    contacts.Count(), location, correlationId);

                return contacts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts by location: {Location} with correlation ID: {CorrelationId}", 
                    location, correlationId);
                return new List<ContactSummary>();
            }
        }
    }

    public async Task<IEnumerable<ContactSummary>> GetAllContactsAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;
        
        using (_logger.BeginScope(new Dictionary<string, object> 
        { 
            ["CorrelationId"] = correlationId,
            ["Operation"] = "GetAllContacts"
        }))
        {
            try
            {
                _logger.LogInformation("Getting all contacts with correlation ID: {CorrelationId}", correlationId);

                // Add correlation ID to request headers
                _client.HttpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
                _client.HttpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

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

                var response = await _client.SendQueryAsync<AllContactsResponse>(query, cancellationToken);

                if (response.Errors?.Any() == true)
                {
                    var errors = string.Join(", ", response.Errors.Select(e => e.Message));
                    _logger.LogError("GraphQL errors for correlation ID {CorrelationId}: {Errors}", 
                        correlationId, errors);
                    return new List<ContactSummary>();
                }

                var contacts = response.Data?.GetContacts?.Select(c => new ContactSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    Company = c.Company,
                    ContactInfos = new()
                }) ?? new List<ContactSummary>();

                _logger.LogInformation("Retrieved {Count} contacts with correlation ID: {CorrelationId}", 
                    contacts.Count(), correlationId);

                return contacts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all contacts with correlation ID: {CorrelationId}", correlationId);
                return new List<ContactSummary>();
            }
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

// Response types for GraphQL