using ContactService.ApiContract.Contracts;
using ContactService.ApiContract.Response.Commands;
using ContactService.Domain.Enums;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Commands;

/// <summary>
/// Command to add contact information
/// </summary>
public class AddContactInfoCommand : ICommand<AddContactInfoResponse>
{
    /// <summary>
    /// Contact ID to add information to
    /// </summary>
    public Guid ContactId { get; set; }
    
    /// <summary>
    /// Type of information (use ContactInfoType enum)
    /// </summary>
    public int InfoType { get; set; }
    
    /// <summary>
    /// Value of the information (phone number, email, etc.)
    /// </summary>
    public string InfoValue { get; set; } = string.Empty;
    
    /// <summary>
    /// Communication type name (for logging purposes)
    /// </summary>
    public string CommunicationType => ((ContactInfoType)InfoType).ToString();
}