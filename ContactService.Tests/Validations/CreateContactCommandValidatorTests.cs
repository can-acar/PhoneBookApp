using ContactService.ApiContract.Request.Commands;
using ContactService.ApiContract.Validations;

namespace ContactService.Tests.Validations;

[Trait("Category", "Unit")]
public class CreateContactCommandValidatorTests
{
    private readonly CreateContactCommandValidator _validator;

    public CreateContactCommandValidatorTests()
    {
        _validator = new CreateContactCommandValidator();
    }

    [Fact]
    public void Validate_ValidCommand_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = "John Doe",
            Company = "Test Company",
            ContactInfos = new List<ContactService.ApiContract.Contracts.CreateContactInfoDto>
            {
                new() { InfoType = 1, InfoValue = "+905551234567" }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullName_ShouldFailValidation(string? name)
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = name!,
            Company = "Test Company",
            ContactInfos = new List<ContactService.ApiContract.Contracts.CreateContactInfoDto>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateContactCommand.FirstName));
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFailValidation()
    {
        // Arrange
        var longName = new string('A', 201); // Exceeds max length of 200
        var command = new CreateContactCommand
        {
            FirstName = longName,
            Company = "Test Company",
            ContactInfos = new List<ContactService.ApiContract.Contracts.CreateContactInfoDto>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateContactCommand.FirstName));
    }

    [Fact]
    public void Validate_CompanyExceedsMaxLength_ShouldFailValidation()
    {
        // Arrange
        var longCompany = new string('B', 201); // Exceeds max length of 200
        var command = new CreateContactCommand
        {
            FirstName = "John Doe",
            Company = longCompany,
            ContactInfos = new List<ContactService.ApiContract.Contracts.CreateContactInfoDto>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateContactCommand.Company));
    }

    [Fact]
    public void Validate_EmptyCompany_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = "John Doe",
            Company = "",
            ContactInfos = new List<ContactService.ApiContract.Contracts.CreateContactInfoDto>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ContactInfosIsNull_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = "John Doe",
            Company = "Test Company",
            ContactInfos = null!
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateContactCommand.ContactInfos));
    }

    [Fact]
    public void Validate_EmptyContactInfos_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateContactCommand
        {
            FirstName = "John Doe",
            Company = "Test Company",
            ContactInfos = new List<ContactService.ApiContract.Contracts.CreateContactInfoDto>()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
