using ContactService.Domain.Entities;
using ContactService.Domain.Enums;

namespace ContactService.Tests.Domain.Entities;

[Trait("Category", "Unit")]
public class ContactInfoTests
{
    private readonly Guid _validContactId = Guid.NewGuid();

    [Fact]
    public void ContactInfo_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange & Act
        var contactInfo = new ContactInfo(_validContactId, ContactInfoType.PhoneNumber, "+905551234567");

        // Assert
        contactInfo.ContactId.Should().Be(_validContactId);
        contactInfo.InfoType.Should().Be(ContactInfoType.PhoneNumber);
        contactInfo.Content.Should().Be("+905551234567");
        contactInfo.Id.Should().NotBeEmpty();
        contactInfo.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ContactInfo_Creation_With_Email_Should_Validate_Format()
    {
        // Arrange & Act
        var contactInfo = new ContactInfo(_validContactId, ContactInfoType.EmailAddress, "test@example.com");

        // Assert
        contactInfo.Content.Should().Be("test@example.com");
    }

    [Fact]
    public void ContactInfo_Creation_With_Phone_Should_Validate_Format()
    {
        // Arrange & Act
        var contactInfo = new ContactInfo(_validContactId, ContactInfoType.PhoneNumber, "+905551234567");

        // Assert
        contactInfo.Content.Should().Be("+905551234567");
    }

    [Theory]
    [InlineData(ContactInfoType.PhoneNumber, "invalid")]
    [InlineData(ContactInfoType.EmailAddress, "invalid-email")]
    [InlineData(ContactInfoType.Location, "")]
    public void ContactInfo_Creation_Should_Throw_Exception_When_Invalid_Content(ContactInfoType infoType, string content)
    {
        // Act & Assert
        Action act = () => new ContactInfo(_validContactId, infoType, content);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateContent_Should_Update_Content_Property()
    {
        // Arrange
        var contactInfo = new ContactInfo(_validContactId, ContactInfoType.PhoneNumber, "+905551234567");
        
        // Act
        string newContent = "+905559998877";
        contactInfo.UpdateContent(newContent);

        // Assert
        contactInfo.Content.Should().Be(newContent);
    }

    [Fact]
    public void UpdateContent_Should_Throw_Exception_When_Invalid_Content()
    {
        // Arrange
        var contactInfo = new ContactInfo(_validContactId, ContactInfoType.PhoneNumber, "+905551234567");
        
        // Act & Assert
        Action act = () => contactInfo.UpdateContent("invalid");
        act.Should().Throw<ArgumentException>();
    }
}
