using ContactService.ApplicationService.Helpers;
using ContactService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ContactService.Tests.Helpers;

public class EnumHelperTests
{
    [Fact]
    public void GetKey_ShouldReturnCorrectKeys()
    {
        // Act & Assert
        ContactInfoType.PhoneNumber.GetKey().Should().Be("PHONE");
        ContactInfoType.EmailAddress.GetKey().Should().Be("EMAIL");
        ContactInfoType.Location.GetKey().Should().Be("LOCATION");
    }

    [Fact]
    public void GetDisplayName_ShouldReturnCorrectDisplayNames()
    {
        // Act & Assert
        ContactInfoType.PhoneNumber.GetDisplayName().Should().Be("Telefon");
        ContactInfoType.EmailAddress.GetDisplayName().Should().Be("E-posta");
        ContactInfoType.Location.GetDisplayName().Should().Be("Konum");
    }

    [Fact]
    public void ParseContactInfoType_ShouldParseCorrectly()
    {
        // Act & Assert
        EnumHelper.ParseContactInfoType("PHONE").Should().Be(ContactInfoType.PhoneNumber);
        EnumHelper.ParseContactInfoType("phone").Should().Be(ContactInfoType.PhoneNumber);
        EnumHelper.ParseContactInfoType("EMAIL").Should().Be(ContactInfoType.EmailAddress);
        EnumHelper.ParseContactInfoType("email").Should().Be(ContactInfoType.EmailAddress);
        EnumHelper.ParseContactInfoType("LOCATION").Should().Be(ContactInfoType.Location);
        EnumHelper.ParseContactInfoType("location").Should().Be(ContactInfoType.Location);
    }

    [Fact]
    public void ParseContactInfoType_WithInvalidValue_ShouldReturnDefault()
    {
        // Act & Assert
        EnumHelper.ParseContactInfoType("INVALID").Should().Be(ContactInfoType.PhoneNumber);
        EnumHelper.ParseContactInfoType("").Should().Be(ContactInfoType.PhoneNumber);
        EnumHelper.ParseContactInfoType(null!).Should().Be(ContactInfoType.PhoneNumber);
    }

    [Fact]
    public void IsValidContactInfoType_ShouldReturnCorrectResult()
    {
        // Act & Assert
        EnumHelper.IsValidContactInfoType("PHONE").Should().BeTrue();
        EnumHelper.IsValidContactInfoType("phone").Should().BeTrue();
        EnumHelper.IsValidContactInfoType("EMAIL").Should().BeTrue();
        EnumHelper.IsValidContactInfoType("email").Should().BeTrue();
        EnumHelper.IsValidContactInfoType("LOCATION").Should().BeTrue();
        EnumHelper.IsValidContactInfoType("location").Should().BeTrue();
        
        EnumHelper.IsValidContactInfoType("INVALID").Should().BeFalse();
        EnumHelper.IsValidContactInfoType("").Should().BeFalse();
        EnumHelper.IsValidContactInfoType(null!).Should().BeFalse();
    }
}
