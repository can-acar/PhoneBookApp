using ContactService.ApiContract.Validations;
using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Contracts;
using FluentValidation.TestHelper;
using Xunit;

namespace ContactService.Tests.Validations;

public class UpdateContactCommandValidatorTests
{
    private readonly UpdateContactCommandValidator _validator;

    public UpdateContactCommandValidatorTests()
    {
        _validator = new UpdateContactCommandValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.Empty,
            FirstName = "John",
            Company = "Tech Corp"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Empty()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = "",
            Company = "Tech Corp"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Is_Null()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = null!,
            Company = "Tech Corp"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_Company_Is_Empty()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = "John",
            Company = ""
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Company);
    }

    [Fact]
    public void Should_Have_Error_When_Company_Is_Null()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = "John",
            Company = null!
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Company);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = "John",
            Company = "Tech Corp"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_FirstName_Exceeds_MaxLength()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = new string('a', 101), // Assuming max length is 100
            Company = "Tech Corp"
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Should_Have_Error_When_Company_Exceeds_MaxLength()
    {
        // Arrange
        var command = new UpdateContactCommand 
        { 
            Id = Guid.NewGuid(),
            FirstName = "John",
            Company = new string('a', 201) // Assuming max length is 200
        };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Company);
    }
}
