using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shared.CrossCutting.Extensions;

/// <summary>
/// Generic enum helper extension for any enum type operations
/// </summary>
public static class EnumHelperExtension
{
    /// <summary>
    /// Gets the string key representation of the enum value
    /// Uses Description attribute if available, otherwise returns the enum name in uppercase
    /// </summary>
    /// <typeparam name="T">Any enum type</typeparam>
    /// <param name="enumValue">The enum value</param>
    /// <returns>String key representation of the enum value</returns>
    public static string GetKey<T>(this T enumValue) where T : Enum
    {
        var memberInfo = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
        
        if (memberInfo != null)
        {
            var descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
                return descriptionAttribute.Description;
        }
        
        return enumValue.ToString().ToUpperInvariant();
    }

    /// <summary>
    /// Gets the enum value from its string representation
    /// </summary>
    /// <typeparam name="T">Any enum type</typeparam>
    /// <param name="value">String representation of the enum</param>
    /// <returns>The corresponding enum value or first enum value if not found</returns>
    public static T GetValue<T>(string value) where T : Enum
    {
        if (string.IsNullOrEmpty(value))
        {
            var values = Enum.GetValues(typeof(T));
            if (values.Length > 0)
            {
                return (T)values.GetValue(0)!;
            }
            throw new ArgumentException($"Cannot get default value for empty enum type {typeof(T).Name}");
        }

        try
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }
        catch
        {
            var values = Enum.GetValues(typeof(T));
            if (values.Length > 0)
            {
                return (T)values.GetValue(0)!;
            }
            throw new ArgumentException($"Cannot parse value '{value}' to enum type {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Gets the display name of the enum value from DisplayName or Description attribute
    /// </summary>
    /// <typeparam name="T">Any enum type</typeparam>
    /// <param name="enumValue">The enum value</param>
    /// <returns>Display name of the enum from attribute or enum string value if no attribute found</returns>
    public static string GetDisplayName<T>(this T enumValue) where T : Enum
    {
        var memberInfo = enumValue.GetType().GetMember(enumValue.ToString()).FirstOrDefault();
        
        if (memberInfo != null)
        {
            var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
                return displayAttribute.Name ?? enumValue.ToString();
            
            var displayNameAttribute = memberInfo.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute != null)
                return displayNameAttribute.DisplayName;
            
            var descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
                return descriptionAttribute.Description;
        }
        
        // Return default string representation if no attributes found
        return enumValue.ToString();
    }

    /// <summary>
    /// Checks if the given string is a valid enum value for type T
    /// </summary>
    /// <typeparam name="T">Any enum type</typeparam>
    /// <param name="value">String value to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidEnumValue<T>(string value) where T : Enum
    {
        if (string.IsNullOrEmpty(value))
            return false;
            
        return Enum.TryParse(typeof(T), value, true, out _);
    }
}
