namespace NotificationService.Domain;

/// <summary>
/// Notification Service message codes and localization
/// </summary>
public static class AppMessage
{
    // Success Messages (2000-2099)
    public const int NotificationSentSuccessfully = 2000;
    public const int NotificationCreatedSuccessfully = 2001;
    public const int NotificationUpdatedSuccessfully = 2002;
    public const int NotificationDeletedSuccessfully = 2003;
    public const int NotificationFoundSuccessfully = 2004;
    public const int NotificationsListedSuccessfully = 2005;
    public const int NotificationChannelConfiguredSuccessfully = 2006;
    public const int NotificationTemplateCreatedSuccessfully = 2007;

    // Validation Errors (2100-2199)
    public const int NotificationMessageRequired = 2100;
    public const int NotificationRecipientRequired = 2101;
    public const int InvalidNotificationId = 2102;
    public const int InvalidNotificationType = 2103;
    public const int InvalidRecipientFormat = 2104;
    public const int NotificationTemplateRequired = 2105;
    public const int InvalidNotificationPriority = 2106;
    public const int InvalidNotificationChannel = 2107;

    // Business Logic Errors (2200-2299)
    public const int NotificationNotFound = 2200;
    public const int NotificationAlreadySent = 2201;
    public const int NotificationChannelNotAvailable = 2202;
    public const int NotificationTemplateNotFound = 2203;
    public const int RecipientNotFound = 2204;
    public const int NotificationQuotaExceeded = 2205;
    public const int NotificationDeliveryFailed = 2206;
    public const int InvalidNotificationStatus = 2207;

    // System Errors (2300-2399)
    public const int EmailServiceUnavailable = 2300;
    public const int SmsServiceUnavailable = 2301;
    public const int DatabaseConnectionError = 2302;
    public const int DatabaseOperationFailed = 2303;
    public const int UnexpectedError = 2304;
    public const int ExternalServiceError = 2305;

    public static Dictionary<int, string> Messages = new()
    {
        // Success Messages
        { NotificationSentSuccessfully, "Bildirim başarıyla gönderildi" },
        { NotificationCreatedSuccessfully, "Bildirim başarıyla oluşturuldu" },
        { NotificationUpdatedSuccessfully, "Bildirim başarıyla güncellendi" },
        { NotificationDeletedSuccessfully, "Bildirim başarıyla silindi" },
        { NotificationFoundSuccessfully, "Bildirim başarıyla bulundu" },
        { NotificationsListedSuccessfully, "Bildirimler başarıyla listelendi" },
        { NotificationChannelConfiguredSuccessfully, "Bildirim kanalı başarıyla yapılandırıldı" },
        { NotificationTemplateCreatedSuccessfully, "Bildirim şablonu başarıyla oluşturuldu" },

        // Validation Errors
        { NotificationMessageRequired, "Bildirim mesajı zorunludur" },
        { NotificationRecipientRequired, "Bildirim alıcısı zorunludur" },
        { InvalidNotificationId, "Geçersiz bildirim kimliği" },
        { InvalidNotificationType, "Geçersiz bildirim türü" },
        { InvalidRecipientFormat, "Geçersiz alıcı formatı" },
        { NotificationTemplateRequired, "Bildirim şablonu zorunludur" },
        { InvalidNotificationPriority, "Geçersiz bildirim önceliği" },
        { InvalidNotificationChannel, "Geçersiz bildirim kanalı" },

        // Business Logic Errors
        { NotificationNotFound, "Bildirim bulunamadı" },
        { NotificationAlreadySent, "Bildirim zaten gönderilmiş" },
        { NotificationChannelNotAvailable, "Bildirim kanalı kullanılamıyor" },
        { NotificationTemplateNotFound, "Bildirim şablonu bulunamadı" },
        { RecipientNotFound, "Alıcı bulunamadı" },
        { NotificationQuotaExceeded, "Bildirim kotası aşıldı" },
        { NotificationDeliveryFailed, "Bildirim teslimi başarısız" },
        { InvalidNotificationStatus, "Geçersiz bildirim durumu" },

        // System Errors
        { EmailServiceUnavailable, "E-posta servisi kullanılamıyor" },
        { SmsServiceUnavailable, "SMS servisi kullanılamıyor" },
        { DatabaseConnectionError, "Veritabanı bağlantı hatası" },
        { DatabaseOperationFailed, "Veritabanı işlemi başarısız" },
        { UnexpectedError, "Beklenmeyen bir hata oluştu" },
        { ExternalServiceError, "Harici servis hatası" }
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
