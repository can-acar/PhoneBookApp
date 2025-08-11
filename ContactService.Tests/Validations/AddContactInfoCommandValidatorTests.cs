using ContactService.ApiContract.Validations;
using ContactService.ApiContract.Request.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace ContactService.Tests.Validations;

public class AddContactInfoCommandValidatorTests
{
    private readonly AddContactInfoCommandValidator _validator;

    public AddContactInfoCommandValidatorTests()
    {
        _validator = new AddContactInfoCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_ContactId_Is_Empty()
    {
        // Arrange
        var command = new AddContactInfoCommand { ContactId = Guid.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ContactId);
    }

    [Fact]
    public void Should_Have_Error_When_InfoType_Is_Invalid()
    {
        // Arrange
        var command = new AddContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            InfoType = 999 // Invalid type
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoType);
    }

    [Fact]
    public void Should_Have_Error_When_InfoValue_Is_Empty()
    {
        // Arrange
        var command = new AddContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            InfoType = 1,
            InfoValue = ""
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoValue);
    }

    [Fact]
    public void Should_Have_Error_When_InfoValue_Is_Null()
    {
        // Arrange
        var command = new AddContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            InfoType = 1,
            InfoValue = null!
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoValue);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new AddContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            InfoType = 1,
            InfoValue = "+905551234567"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_InfoValue_Exceeds_MaxLength()
    {
        // Arrange
        var command = new AddContactInfoCommand 
        { 
            ContactId = Guid.NewGuid(),
            InfoType = 1,
            InfoValue = new string('a', 256) // Exceeds max length
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoValue);
    }
}
