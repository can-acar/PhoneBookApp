using ContactService.Domain.Entities;
using ContactService.Domain.Enums;

namespace ContactService.Tests.Domain.Entities;

[Trait("Category", "Unit")]
public class ContactTests
{
    [Fact]
    public void Contact_Creation_Should_Set_Properties_Correctly()
    {
        // Arrange & Act
        string firstName = "John";
        string lastName = "Doe";
        string company = "ACME Corp";
        var contact = new Contact(firstName, lastName, company);

        // Assert
        contact.FirstName.Should().Be(firstName);
        contact.LastName.Should().Be(lastName);
        contact.Company.Should().Be(company);
        contact.Id.Should().NotBeEmpty();
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        contact.ContactInfos.Should().NotBeNull();
        contact.ContactInfos.Should().BeEmpty();
    }

    [Fact]
    public void FullName_WhenBothNamesProvided_ShouldReturnCombinedName()
    {
        // Arrange
        var contact = new Contact("John", "Doe", "ACME Corp");

        // Act
        var fullName = contact.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Theory]
    [InlineData("", "Doe", "ACME")]
    [InlineData(" ", "Doe", "ACME")]
    [InlineData("John", "", "ACME")]
    [InlineData("John", " ", "ACME")]
    public void Contact_Creation_Should_Throw_Exception_When_Invalid_Name(string firstName, string lastName, string company)
    {
        // Act & Assert
        Action act = () => new Contact(firstName, lastName, company);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddContactInfo_Should_Add_Info_To_Collection()
    {
        // Arrange
        var contact = new Contact("John", "Doe", "ACME Corp");
        var contactId = contact.Id;
        
        // Act
        contact.AddContactInfo(ContactInfoType.PhoneNumber, "+905551234567");

        // Assert
        contact.ContactInfos.Should().HaveCount(1);
        var contactInfo = contact.ContactInfos.First();
        contactInfo.ContactId.Should().Be(contactId);
        contactInfo.InfoType.Should().Be(ContactInfoType.PhoneNumber);
        contactInfo.Content.Should().Be("+905551234567");
    }

    [Theory]
    [InlineData(ContactInfoType.PhoneNumber, "InvalidPhone")]
    [InlineData(ContactInfoType.EmailAddress, "invalid-email")]
    [InlineData(ContactInfoType.Location, "")]
    public void AddContactInfo_Should_Throw_Exception_When_Invalid_Content(ContactInfoType infoType, string content)
    {
        // Arrange
        var contact = new Contact("John", "Doe", "ACME Corp");

        // Act & Assert
        Action act = () => contact.AddContactInfo(infoType, content);
        act.Should().Throw<ArgumentException>();
    }
    
    [Fact]
    public void UpdateContactInformation_Should_Update_Properties()
    {
        // Arrange
        var contact = new Contact("John", "Doe", "ACME Corp");
        
        // Act
        string newFirstName = "Jane";
        string newLastName = "Smith";
        string newCompany = "New Corp";
        contact.UpdateContactInformation(newFirstName, newLastName, newCompany);

        // Assert
        contact.FirstName.Should().Be(newFirstName);
        contact.LastName.Should().Be(newLastName);
        contact.Company.Should().Be(newCompany);
    }

    [Fact]
    public void RemoveContactInfo_Should_Remove_Info_From_Collection()
    {
        // Arrange
        var contact = new Contact("John", "Doe", "ACME Corp");
        contact.AddContactInfo(ContactInfoType.PhoneNumber, "+905551234567");
        var contactInfo = contact.ContactInfos.First();
        
        // Act
        contact.RemoveContactInfo(contactInfo.Id);

        // Assert
        contact.ContactInfos.Should().BeEmpty();
    }

    [Fact]
    public void RemoveContactInfo_Should_Throw_Exception_When_Info_Not_Found()
    {
        // Arrange
        var contact = new Contact("John", "Doe", "ACME Corp");
        
        // Act & Assert
        Action act = () => contact.RemoveContactInfo(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*bulunamadı*");
    }
}
