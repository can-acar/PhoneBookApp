using ContactService.ApiContract.Contracts;

namespace ContactService.ApiContract.Response.Queries;

/// <summary>
/// Response model for contacts by location query
/// </summary>
public class ContactsByLocationResponse
{
    /// <summary>
    /// List of contacts at the specified location
    /// </summary>
    public List<ContactDto> Contacts { get; set; } = new();
    
    /// <summary>
    /// Total count of contacts at this location
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Current page number
    /// </summary>
    public int CurrentPage { get; set; }
    
    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// The location searched for
    /// </summary>
    public string Location { get; set; } = string.Empty;
}
