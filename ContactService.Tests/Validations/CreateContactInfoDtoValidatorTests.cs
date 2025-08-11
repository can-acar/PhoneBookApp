using ContactService.ApiContract.Validations;
using ContactService.ApiContract.Contracts;
using ContactService.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;
using ContactService.ApplicationService.Helpers;
using ContactService.Domain;

namespace ContactService.Tests.Validations;

public class CreateContactInfoDtoValidatorTests
{
    private readonly CreateContactInfoDtoValidator _validator;

    public CreateContactInfoDtoValidatorTests()
    {
        _validator = new CreateContactInfoDtoValidator();
    }

    [Fact]
    public void Should_Have_Error_When_InfoValue_Is_Empty()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = ""
        };

        // Act & Assert
        _validator.TestValidate(dto)
            .ShouldHaveValidationErrorFor(x => x.InfoValue)
            .WithErrorMessage("İletişim içeriği boş olamaz");
    }

    [Fact]
    public void Should_Have_Error_When_InfoValue_Is_Null()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = null!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoValue);
    }

    [Fact]
    public void Should_Have_Error_When_InfoValue_Exceeds_MaxLength()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = new string('1', 256) // 256 characters
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoValue)
            .WithErrorMessage(AppMessage.InvalidPhoneNumber.GetMessage());
    }

    [Fact]
    public void Should_Have_Error_When_InfoType_Is_Invalid()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = 99, // Invalid type
            InfoValue = "test@example.com"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoType)
            .WithErrorMessage("Geçersiz iletişim türü");
    }

    [Theory]
    [InlineData((int)ContactInfoType.PhoneNumber)]
    [InlineData((int)ContactInfoType.EmailAddress)]
    [InlineData((int)ContactInfoType.Location)]
    public void Should_Not_Have_Error_When_InfoType_Is_Valid(int infoType)
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = infoType,
            InfoValue = "valid value"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.InfoType);
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid_Phone()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = "+905551234567"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid_Email()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.EmailAddress,
            InfoValue = "test@example.com"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid_Location()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.Location,
            InfoValue = "Istanbul, Turkey"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Trim_Whitespace_And_Pass_Validation()
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.EmailAddress,
            InfoValue = "  test@example.com  "
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Should_Have_Error_When_InfoValue_Is_Whitespace_Only(string whitespaceValue)
    {
        // Arrange
        var dto = new CreateContactInfoDto
        {
            InfoType = (int)ContactInfoType.PhoneNumber,
            InfoValue = whitespaceValue
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InfoValue);
    }
}
