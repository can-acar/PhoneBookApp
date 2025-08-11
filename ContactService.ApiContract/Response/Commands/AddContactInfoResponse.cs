using ContactService.ApiContract.Contracts;

namespace ContactService.ApiContract.Response.Commands;

/// <summary>
/// Response model for adding contact information
/// </summary>
public class AddContactInfoResponse
{
    /// <summary>
    /// The ID of the contact information that was added
    /// </summary>
    public Guid ContactInfoId { get; set; }
    
    /// <summary>
    /// The ID of the contact to which the information was added
    /// </summary>
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// The contact information that was added
    /// </summary>
    public ContactInfoDto ContactInfo { get; set; } = null!;
}
