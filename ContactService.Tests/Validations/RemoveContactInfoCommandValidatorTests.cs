using ContactService.ApiContract.Validations;
using ContactService.ApiContract.Request.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace ContactService.Tests.Validations;

public class RemoveContactInfoCommandValidatorTests
{
    private readonly RemoveContactInfoCommandValidator _validator;

    public RemoveContactInfoCommandValidatorTests()
    {
        _validator = new RemoveContactInfoCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_ContactId_Is_Empty()
    {
        // Arrange
        var command = new RemoveContactInfoCommand 
        { 
            ContactId = Guid.Empty,
            ContactInfoId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContactId);
    }

    [Fact]
    public void Should_Have_Error_When_ContactInfoId_Is_Empty()
    {
        // Arrange
        var command = new RemoveContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            ContactInfoId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContactInfoId);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new RemoveContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            ContactInfoId = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Both_Ids_Are_Empty()
    {
        // Arrange
        var command = new RemoveContactInfoCommand 
        { 
            ContactId = Guid.Empty,
            ContactInfoId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContactId);
        result.ShouldHaveValidationErrorFor(x => x.ContactInfoId);
    }
}
