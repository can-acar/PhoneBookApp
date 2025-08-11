using ContactService.Domain.Enums;

namespace ContactService.ApplicationService.Helpers;

/// <summary>
/// Enum helper class providing extension methods for enum operations
/// </summary>
public static class EnumHelper
{
    /// <summary>
    /// Gets the string key representation of ContactInfoType enum
    /// </summary>
    /// <param name="infoType">The ContactInfoType enum value</param>
    /// <returns>String representation of the enum</returns>
    public static string GetKey(this ContactInfoType infoType)
    {
        return infoType switch
        {
            ContactInfoType.PhoneNumber => "PHONE",
            ContactInfoType.EmailAddress => "EMAIL", 
            ContactInfoType.Location => "LOCATION",
            _ => "UNKNOWN"
        };
    }

    /// <summary>
    /// Gets the display name of ContactInfoType enum
    /// </summary>
    /// <param name="infoType">The ContactInfoType enum value</param>
    /// <returns>Display name of the enum</returns>
    public static string GetDisplayName(this ContactInfoType infoType)
    {
        return infoType switch
        {
            ContactInfoType.PhoneNumber => "Telefon",
            ContactInfoType.EmailAddress => "E-posta",
            ContactInfoType.Location => "Konum",
            _ => "Bilinmeyen"
        };
    }

    /// <summary>
    /// Parses string to ContactInfoType enum
    /// </summary>
    /// <param name="value">String value to parse</param>
    /// <returns>ContactInfoType enum value</returns>
    public static ContactInfoType ParseContactInfoType(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "PHONE" => ContactInfoType.PhoneNumber,
            "EMAIL" => ContactInfoType.EmailAddress,
            "LOCATION" => ContactInfoType.Location,
            _ => ContactInfoType.PhoneNumber // Default value
        };
    }

    /// <summary>
    /// Checks if the given string is a valid ContactInfoType
    /// </summary>
    /// <param name="value">String value to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidContactInfoType(string value)
    {
        return value?.ToUpperInvariant() switch
        {
            "PHONE" or "EMAIL" or "LOCATION" => true,
            _ => false
        };
    }
}
