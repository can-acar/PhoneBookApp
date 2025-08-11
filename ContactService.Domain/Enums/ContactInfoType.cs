using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ContactService.Domain.Enums;

public enum ContactInfoType
{
    [Display(Name = "Telefon")]
    [Description("PHONE")]
    PhoneNumber = 1,
    
    [Display(Name = "E-posta")]
    [Description("EMAIL")]
    EmailAddress = 2,
    
    [Display(Name = "Konum")]
    [Description("LOCATION")]
    Location = 3
}