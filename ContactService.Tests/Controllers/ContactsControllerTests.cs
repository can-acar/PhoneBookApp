using ContactService.Api.Controllers;
using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Request.Queries;
using ContactService.ApiContract.Response.Commands;
using ContactService.ApiContract.Response.Queries;
using ContactService.ApiContract.Contracts;
using ContactService.Domain;
using ContactService.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shared.CrossCutting.CorrelationId;
using Shared.CrossCutting.Models;
using FluentAssertions;
using Moq;
using Shared.CrossCutting.Extensions;
using Xunit;

namespace ContactService.Tests.Controllers;

public class ContactsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ICorrelationIdProvider> _mockCorrelationIdProvider;
    private readonly Mock<ILogger<ContactsController>> _mockLogger;
    private readonly ContactsController _controller;

    public ContactsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockCorrelationIdProvider = new Mock<ICorrelationIdProvider>();
        _mockLogger = new Mock<ILogger<ContactsController>>();

        _mockCorrelationIdProvider.Setup(x => x.CorrelationId).Returns(Guid.NewGuid().ToString());

        _controller = new ContactsController(_mockMediator.Object, _mockCorrelationIdProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAllContacts_WhenSuccessful_ReturnsOkResult()
    {
        // Arrange
        var list = new List<ContactSummaryDto>
        {
            new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Company = "Test Company" }
        };
        var pageResponse = PageResponse.Result(
            list
            , 1, 10, 1, AppMessage.ContactsListedSuccessfully, "Contacts listed successfully");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllContactsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageResponse);

        // Act  
        var result = await _controller.GetAllContacts();

        // Assert
        result.Should().BeOfType<ActionResult<GetAllContactsResponse>>();
        var actionResult = result as ActionResult<GetAllContactsResponse>;
        var okResult = actionResult?.Result as OkObjectResult;
        okResult?.Value.Should().Be(pageResponse);
    }

    [Fact]
    public async Task GetAllContacts_WhenFailed_ReturnsServerError()
    {
        // Arrange
        var pageResponse = PageResponse.Result(
            new List<ContactSummaryDto>()
            , 1, 0, 1, AppMessage.UnexpectedError, "Server error");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllContactsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageResponse);

        // Act
        var result = await _controller.GetAllContacts();

        // Assert
        result.Should().BeOfType<ActionResult<GetAllContactsResponse>>();
        var actionResult = result as ActionResult<GetAllContactsResponse>;
        var okResult = actionResult?.Result as OkObjectResult;
        okResult?.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContactById_WhenContactExists_ReturnsOkResult()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var contactDto = new ContactDto
        {
            Id = contactId,
            FirstName = "John",
            LastName = "Doe",
            Company = "Test Company"
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetContactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactDto);

        // Act
        var result = await _controller.GetContactById(contactId);

        // Assert
        result.Should().BeOfType<ActionResult<GetContactByIdResponse>>();
        var actionResult = result as ActionResult<GetContactByIdResponse>;
        var okResult = actionResult?.Result as OkObjectResult;
        okResult?.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContactById_WhenContactNotFound_ReturnsNotFound()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        _mockMediator.Setup(m => m.Send(It.IsAny<GetContactByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContactDto?)null);

        // Act
        var result = await _controller.GetContactById(contactId);

        // Assert
        result.Should().BeOfType<ActionResult<GetContactByIdResponse>>();
        var actionResult = result as ActionResult<GetContactByIdResponse>;
        actionResult?.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateContact_WhenSuccessful_ReturnsCreated()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Company = "Test Company"
        };
        var contactId = Guid.NewGuid();
        var contactDto = new ContactDto { Id = contactId, FirstName = "John", LastName = "Doe", Company = "Test Company" };
        var response = ApiResponse.Result(
            true,
            contactDto.Id,
            AppMessage.ContactCreatedSuccessfully,
            AppMessage.ContactCreatedSuccessfully.GetMessage());


        _mockMediator.Setup(m => m.Send(It.IsAny<CreateContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateContact(command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var responseValue = actionResult?.Value as ApiResponse<Guid>;
        responseValue.Should().NotBeNull();
        responseValue!.Success.Should().BeTrue();
        responseValue.Data.Should().Be(contactId);
        responseValue.Code.Should().Be(AppMessage.ContactCreatedSuccessfully);
    }

    [Fact]
    public async Task CreateContact_WhenFailed_ReturnsCreatedWithEmptyId()
    {
        // Arrange
        var command = new CreateContactCommand();
        var failedResponse = ApiResponse.Result<Guid>(
            false,
            Guid.Empty,
            AppMessage.CreateContactFailed,
            AppMessage.CreateContactFailed.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<CreateContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _controller.CreateContact(command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var response = actionResult?.Value as ApiResponse<Guid>;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Data.Should().Be(Guid.Empty);
        response.Message.Should().Be(AppMessage.CreateContactFailed.GetMessage());
    }

    [Fact]
    public async Task UpdateContact_WhenSuccessful_ReturnsOk()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var command = new UpdateContactCommand
        {
            Id = contactId,
            FirstName = "Jane",
            LastName = "Doe",
            Company = "Updated Company"
        };

        var expectedResponse = ApiResponse.Result(
            true,
            contactId,
            AppMessage.ContactUpdatedSuccessfully,
            AppMessage.ContactUpdatedSuccessfully.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateContact(contactId, command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var responseValue = actionResult?.Value as ApiResponse<Guid>;
        responseValue.Should().NotBeNull();
        responseValue!.Success.Should().BeTrue();
        responseValue.Data.Should().Be(contactId);
        responseValue.Code.Should().Be(AppMessage.ContactUpdatedSuccessfully);
    }

    [Fact]
    public async Task UpdateContact_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var command = new UpdateContactCommand { Id = contactId };
        var expectedResponse = ApiResponse.Result(
            false,
            contactId,
            AppMessage.NotFoundData,
            AppMessage.NotFoundData.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UpdateContact(contactId, command);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var responseValue = actionResult?.Value as ApiResponse<Guid>;
        responseValue.Should().NotBeNull();
        responseValue!.Success.Should().BeFalse();
        responseValue.Data.Should().Be(Guid.Empty);
        responseValue.Code.Should().Be(AppMessage.NotFoundData);
    }

    [Fact]
    public async Task DeleteContact_WhenSuccessful_ReturnsNoContent()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var expectedResponse = ApiResponse.Result(
            true,
            true,
            AppMessage.ContactDeletedSuccessfully,
            AppMessage.ContactDeletedSuccessfully.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteContact(contactId);

        // Assert
        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var responseValue = actionResult?.Value as ApiResponse<bool>;
        responseValue.Should().NotBeNull();
        responseValue!.Success.Should().BeTrue();
        responseValue.Data.Should().Be(true);
        responseValue.Code.Should().Be(AppMessage.ContactDeletedSuccessfully);
    }

    [Fact]
    public async Task DeleteContact_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var expectedResponse = ApiResponse.Result(
            false,
            false,
            AppMessage.NotFoundData,
            AppMessage.NotFoundData.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<DeleteContactCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteContact(contactId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var responseValue = actionResult?.Value as ApiResponse<bool>;
        responseValue.Should().NotBeNull();
        responseValue!.Success.Should().BeFalse();
        responseValue.Data.Should().Be(false);
        responseValue.Code.Should().Be(AppMessage.NotFoundData);
    }

    [Fact]
    public async Task AddContactInfo_WhenSuccessful_ReturnsCreated()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var command = new AddContactInfoCommand
        {
            ContactId = contactId,
            InfoType = 1, // PHONE
            InfoValue = "+905551234567"
        };
        var communicationId = Guid.NewGuid();
        var contactInfo = new ContactInfoDto
        {
            Id = communicationId,
            InfoType = ContactInfoType.PhoneNumber.GetKey(),
            InfoValue = command.InfoValue
        };
        var response = new AddContactInfoResponse
        {
            ContactId = contactId,
            ContactInfo = contactInfo
        };
        var expectedResponse = ApiResponse.Result(true,
            response,
            AppMessage.ContactInfoAddedSuccessfully,
            AppMessage.ContactInfoAddedSuccessfully.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<AddContactInfoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.AddContactInfo(contactId, command);

        // Assert
        result.Should().BeOfType<ActionResult<AddContactInfoResponse>>();
        var actionResult = result as ActionResult<AddContactInfoResponse>;
        var createdResult = actionResult?.Result as CreatedAtActionResult;
        createdResult?.ActionName.Should().Be(nameof(ContactsController.GetContactById));
        createdResult?.RouteValues?["id"].Should().Be(contactId);
        createdResult?.Value.Should().Be(expectedResponse);
        command.ContactId.Should().Be(contactId);
    }

    [Fact]
    public async Task RemoveContactInfo_WhenSuccessful_ReturnsNoContent()
    {
        // Arrange
        var contactId = Guid.NewGuid();
        var communicationId = Guid.NewGuid();
        var expectedResponse = ApiResponse.Result(true,
            true,
            AppMessage.ContactInfoDeletedSuccessfully,
            AppMessage.ContactInfoDeletedSuccessfully.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<RemoveContactInfoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RemoveContactInfo(contactId, communicationId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetContactsByLocation_WhenSuccessful_ReturnsOkResult()
    {
        // Arrange
        var location = "Istanbul";
        var query = new GetContactsByLocationQuery { Location = location };
        var pageResponse = PageResponse.Result(
            new List<ContactDto>
            {
                new() { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Company = "Test Company" }
            },
            1, 1, 1, AppMessage.ContactsListedSuccessfully, "Contacts found");

        _mockMediator.Setup(m => m.Send(It.IsAny<GetContactsByLocationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageResponse);

        // Act
        var result = await _controller.GetContactsByLocation(location);

        // Assert
        result.Should().BeOfType<ActionResult<ContactsByLocationResponse>>();
        var actionResult = result as ActionResult<ContactsByLocationResponse>;
        var okResult = actionResult?.Result as OkObjectResult;
        okResult?.Value.Should().Be(pageResponse);
    }

    [Fact]
    public async Task GetLocationStatistics_WhenSuccessful_ReturnsOkResult()
    {
        // Arrange
        var query = new GetLocationStatisticsQuery();
        var statisticsResponse = new List<LocationStatisticsResponse>
        {
            new() { Location = "Istanbul", ContactCount = 10, PhoneNumberCount = 15 }
        };
        var expectedResponse = ApiResponse.Result(
            true,
            statisticsResponse,
            AppMessage.LocationStatisticsRetrievedSuccessfully,
            AppMessage.LocationStatisticsRetrievedSuccessfully.GetMessage());

        _mockMediator.Setup(m => m.Send(It.IsAny<GetLocationStatisticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetLocationStatistics();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var actionResult = result as OkObjectResult;
        var response = actionResult?.Value as ApiResponse<List<LocationStatisticsResponse>>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data.Should().BeEquivalentTo(statisticsResponse);
        response.Code.Should().Be(AppMessage.LocationStatisticsRetrievedSuccessfully);
        response.Message.Should().Be(AppMessage.LocationStatisticsRetrievedSuccessfully.GetMessage());
    }
}