using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Request.Queries;
using ContactService.ApiContract.Response.Commands;
using ContactService.ApiContract.Response.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shared.CrossCutting.CorrelationId;
using Shared.CrossCutting.Models;

namespace ContactService.Api.Controllers;

/// <summary>
/// REST API controller for contact management operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ContactsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(
        IMediator mediator,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<ContactsController> logger)
    {
        _mediator = mediator;
        _correlationIdProvider = correlationIdProvider;
        _logger = logger;
    }

    /// <summary>
    /// Get all contacts with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllContactsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GetAllContactsResponse>> GetAllContacts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Getting all contacts - Page: {Page}, PageSize: {PageSize}, CorrelationId: {CorrelationId}",
            page, pageSize, correlationId);

        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and PageSize must be greater than 0");
        }

        var query = new GetAllContactsQuery
        {
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Limit page size to 100
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetContactByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetContactByIdResponse>> GetContactById(Guid id, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Getting contact by ID: {ContactId}, CorrelationId: {CorrelationId}",
            id, correlationId);

        var query = new GetContactByIdQuery { Id = id };
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null || result.Data == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new contact
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateContact([FromBody] CreateContactCommand command, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Creating contact: {Name}, Company: {Company}, CorrelationId: {CorrelationId}",
            command.FirstName, command.Company, correlationId);

        var result = await _mediator.Send(command, cancellationToken);


        return Ok(result);
    }

    /// <summary>
    /// Update an existing contact
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] UpdateContactCommand command, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Updating contact: {ContactId}, CorrelationId: {CorrelationId}",
            id, correlationId);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Delete a contact
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(Guid id, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Deleting contact: {ContactId}, CorrelationId: {CorrelationId}",
            id, correlationId);

        var command = new DeleteContactCommand { Id = id };
        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Add communication information to a contact
    /// </summary>
    [HttpPost("{id:guid}/contact-info")]
    [ProducesResponseType(typeof(AddContactInfoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AddContactInfoResponse>> AddContactInfo(Guid id, [FromBody] AddContactInfoCommand command, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        if (id != command.ContactId)
        {
            return BadRequest("Contact ID in URL must match Contact ID in request body");
        }

        _logger.LogInformation("Adding communication info to contact: {ContactId}, Type: {Type}, CorrelationId: {CorrelationId}",
            id, command.CommunicationType, correlationId);

        var result = await _mediator.Send(command, cancellationToken);

        if (result == null || result.Data == null)
        {
            return NotFound();
        }

        return CreatedAtAction(nameof(GetContactById), new { id = id }, result);
    }

    /// <summary>
    /// Remove communication information from a contact
    /// </summary>
    [HttpDelete("{contactId:guid}/contact-info/{infoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveContactInfo(Guid contactId, Guid infoId, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Removing communication info: {InfoId} from contact: {ContactId}, CorrelationId: {CorrelationId}",
            infoId, contactId, correlationId);

        var command = new RemoveContactInfoCommand
        {
            ContactId = contactId,
            ContactInfoId = infoId
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Get contacts by location
    /// </summary>
    [HttpGet("location/{location}")]
    [ProducesResponseType(typeof(ContactsByLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactsByLocationResponse>> GetContactsByLocation(string location, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        if (string.IsNullOrWhiteSpace(location))
        {
            return BadRequest("Location parameter is required and cannot be empty");
        }

        if (page < 1 || pageSize < 1)
        {
            return BadRequest("Page and PageSize must be greater than 0");
        }

        _logger.LogInformation("Getting contacts by location: {Location}, Page: {Page}, PageSize: {PageSize}, CorrelationId: {CorrelationId}",
            location, page, pageSize, correlationId);

        var query = new GetContactsByLocationQuery
        {
            Location = location,
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Limit page size to 100
        };
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get location statistics
    /// </summary>
    [HttpGet("statistics/locations")]
    [ProducesResponseType(typeof(ApiResponse<List<LocationStatisticsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocationStatistics(CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdProvider.CorrelationId;

        _logger.LogInformation("Getting location statistics, CorrelationId: {CorrelationId}", correlationId);

        var query = new GetLocationStatisticsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }
}