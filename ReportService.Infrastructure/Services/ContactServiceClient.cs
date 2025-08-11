using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ReportService.Domain.Entities;
using ReportService.Domain.Interfaces;
using Shared.CrossCutting.CorrelationId;
using System.Text.Json;
using System.Net.Http;

namespace ReportService.Infrastructure.Services;

/// <summary>
/// HTTP client implementation for communicating with ContactService
/// Clean Architecture: Infrastructure layer service for external service communication
/// </summary>
public class ContactServiceClient : IContactServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContactServiceClient> _logger;
    private readonly ICorrelationIdProvider _correlationIdProvider;

    public ContactServiceClient(
        HttpClient httpClient,
        ILogger<ContactServiceClient> logger,
        ICorrelationIdProvider correlationIdProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
        _correlationIdProvider = correlationIdProvider;
    }

    /// <summary>
    /// Get contacts by location using REST API
    /// </summary>
    public async Task<IEnumerable<ContactSummary>> GetContactsByLocationAsync(
        string location, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting contacts by location: {Location}", location);

            // Add correlation ID header
            var correlationId = _correlationIdProvider.Get();
            if (!string.IsNullOrEmpty(correlationId))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
                _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            }

            // Call REST API endpoint
            var url = $"api/v1/contacts/by-location/{Uri.EscapeDataString(location)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get contacts by location: {Location}. Status: {Status}", 
                    location, response.StatusCode);
                return new List<ContactSummary>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var contactsResponse = JsonSerializer.Deserialize<ContactsByLocationApiResponse>(jsonContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var contacts = new List<ContactSummary>();
            if (contactsResponse?.Data != null)
            {
                foreach (var c in contactsResponse.Data)
                {
                    var contact = new ContactSummary
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Company = c.Company
                    };
                    contacts.Add(contact);
                }
            }

            _logger.LogInformation("Found {Count} contacts for location: {Location}", 
                contacts.Count(), location);

            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts by location: {Location}", location);
            throw;
        }
    }

    /// <summary>
    /// Get statistics for all locations using REST API
    /// </summary>
    public async Task<List<LocationStatistic>> GetAllLocationStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting location statistics");

            // Add correlation ID header
            var correlationId = _correlationIdProvider.Get();
            if (!string.IsNullOrEmpty(correlationId))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
                _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            }

            // Call REST API endpoint
            var url = "api/v1/contacts/location-statistics";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get location statistics. Status: {Status}", response.StatusCode);
                return new List<LocationStatistic>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var statisticsResponse = JsonSerializer.Deserialize<LocationStatisticsApiResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var statistics = new List<LocationStatistic>();
            if (statisticsResponse?.Data != null)
            {
                foreach (var stat in statisticsResponse.Data)
                {
                    statistics.Add(new LocationStatistic(
                        stat.Location, 
                        stat.ContactCount,
                        stat.PhoneNumberCount
                    ));
                }
            }

            _logger.LogInformation("Found statistics for {Count} locations", statistics.Count);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location statistics");
            throw;
        }
    }

    /// <summary>
    /// Get all contacts using REST API
    /// </summary>
    public async Task<IEnumerable<ContactSummary>> GetAllContactsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting all contacts");

            // Add correlation ID header
            var correlationId = _correlationIdProvider.Get();
            if (!string.IsNullOrEmpty(correlationId))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
                _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            }

            // Call REST API endpoint with pagination
            var url = "api/v1/contacts?page=1&pageSize=1000";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get all contacts. Status: {Status}", response.StatusCode);
                return new List<ContactSummary>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var contactsResponse = JsonSerializer.Deserialize<AllContactsApiResponse>(jsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var contacts = new List<ContactSummary>();
            if (contactsResponse?.Data?.Items != null)
            {
                foreach (var c in contactsResponse.Data.Items)
                {
                    var contact = new ContactSummary
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Company = c.Company
                    };
                    contacts.Add(contact);
                }
            }

            _logger.LogInformation("Found {Count} total contacts", contacts.Count());

            return contacts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all contacts");
            throw;
        }
    }
}

// REST API Response Models
public class ContactsByLocationApiResponse
{
    public List<ContactDto> Data { get; set; } = new List<ContactDto>();
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LocationStatisticsApiResponse
{
    public List<LocationStatisticDto> Data { get; set; } = new List<LocationStatisticDto>();
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AllContactsApiResponse
{
    public PagedContactsDto Data { get; set; } = new PagedContactsDto();
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class PagedContactsDto
{
    public List<ContactDto> Items { get; set; } = new List<ContactDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
}

public class ContactDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}

public class LocationStatisticDto
{
    public string Location { get; set; } = string.Empty;
    public int ContactCount { get; set; }
    public int PhoneNumberCount { get; set; }
}
