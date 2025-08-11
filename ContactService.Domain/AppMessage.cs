namespace ContactService.Domain;

/// <summary>
/// Contact Service message codes and localization
/// </summary>
public static class AppMessage
{
    // Success Messages (1000-1099)
    public const int ContactCreatedSuccessfully = 1000;
    public const int ContactUpdatedSuccessfully = 1001;
    public const int ContactDeletedSuccessfully = 1002;
    public const int ContactFoundSuccessfully = 1003;
    public const int ContactsListedSuccessfully = 1004;
    public const int ContactInfoAddedSuccessfully = 1005;
    public const int ContactInfoUpdatedSuccessfully = 1006;
    public const int ContactInfoDeletedSuccessfully = 1007;
    public const int ContactInfoRemoved = 1008;
    public const int LocationStatisticsRetrievedSuccessfully = 1009;

    // Validation Errors (1100-1199)
    public const int ContactNameRequired = 1100;
    public const int ContactSurnameRequired = 1101;
    public const int ContactCompanyRequired = 1102;
    public const int InvalidContactId = 1103;
    public const int InvalidContactInfoType = 1104;
    public const int ContactInfoValueRequired = 1105;
    public const int InvalidPhoneNumber = 1106;
    public const int InvalidEmailAddress = 1107;

    // Business Logic Errors (1200-1299)
    public const int ContactNotFound = 1200;
    public const int ContactInfoNotFound = 1201;
    public const int ContactAlreadyExists = 1202;
    public const int ContactInfoAlreadyExists = 1203;
    public const int ContactHasActiveInfo = 1204;
    public const int DuplicateContactInfo = 1205;
    public const int NotFoundData = 1206;
    public const int CreateContactFailed = 1207;
    public const int UpdateContactFailed = 1208;
    public const int DeleteContactFailed = 1209;
    public const int InvalidContactInfo = 1210;

    // System Errors (1300-1399)
    public const int DatabaseConnectionError = 1300;
    public const int DatabaseOperationFailed = 1301;
    public const int UnexpectedError = 1302;


    public static Dictionary<int, string> Messages = new()
    {
        // Success Messages
        { ContactCreatedSuccessfully, "Kişi başarıyla oluşturuldu" },
        { ContactUpdatedSuccessfully, "Kişi başarıyla güncellendi" },
        { ContactDeletedSuccessfully, "Kişi başarıyla silindi" },
        { ContactFoundSuccessfully, "Kişi başarıyla bulundu" },
        { ContactsListedSuccessfully, "Kişiler başarıyla listelendi" },
        { ContactInfoAddedSuccessfully, "Kişi bilgisi başarıyla eklendi" },
        { ContactInfoUpdatedSuccessfully, "Kişi bilgisi başarıyla güncellendi" },
        { ContactInfoDeletedSuccessfully, "Kişi bilgisi başarıyla silindi" },
        { LocationStatisticsRetrievedSuccessfully, "Konum istatistikleri başarıyla alındı" },
        { ContactInfoRemoved, "Kişi bilgisi başarıyla kaldırıldı" },


        // Validation Errors
        { ContactNameRequired, "Kişi adı zorunludur" },
        { ContactSurnameRequired, "Kişi soyadı zorunludur" },
        { ContactCompanyRequired, "Şirket adı zorunludur" },
        { InvalidContactId, "Geçersiz kişi kimliği" },
        { InvalidContactInfoType, "Geçersiz kişi bilgi türü" },
        { ContactInfoValueRequired, "Kişi bilgi değeri zorunludur" },
        { InvalidPhoneNumber, "Geçersiz telefon numarası formatı" },
        { InvalidEmailAddress, "Geçersiz e-posta adresi formatı" },

        // Business Logic Errors
        { ContactNotFound, "Kişi bulunamadı" },
        { ContactInfoNotFound, "Kişi bilgisi bulunamadı" },
        { ContactAlreadyExists, "Kişi zaten mevcut" },
        { ContactInfoAlreadyExists, "Kişi bilgisi zaten mevcut" },
        { ContactHasActiveInfo, "Kişinin aktif bilgileri bulunduğu için silinemez" },
        { DuplicateContactInfo, "Tekrarlanan kişi bilgisi" },
        { NotFoundData, "Kayıt bulunamadı" },
        { UpdateContactFailed, "Kişi güncelleme işlemi başarısız oldu" },
        { CreateContactFailed, "Kişi oluşturma işlemi başarısız oldu" },
        { DeleteContactFailed, "Kişi silme işlemi başarısız oldu" },
        { InvalidContactInfo, "Geçersiz kişi bilgisi" },


        // System Errors
        { DatabaseConnectionError, "Veritabanı bağlantı hatası" },
        { DatabaseOperationFailed, "Veritabanı işlemi başarısız" },
        { UnexpectedError, "Beklenmeyen bir hata oluştu" },
    };

    /// <summary>
    /// Get message text by code
    /// </summary>
    /// <param name="code">Message code</param>
    /// <returns>Message text</returns>
    public static string GetMessage(this int code)
    {
        return Messages.TryGetValue(code, out var message) ? message : $"Unknown message code: {code}";
    }
}