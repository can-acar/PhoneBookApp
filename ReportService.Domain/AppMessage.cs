namespace ReportService.Domain;

/// <summary>
/// Report Service message codes and localization
/// </summary>
public static class AppMessage
{
    // Success Messages (3000-3099)
    public const int ReportGeneratedSuccessfully = 3000;
    public const int ReportCreatedSuccessfully = 3001;
    public const int ReportUpdatedSuccessfully = 3002;
    public const int ReportDeletedSuccessfully = 3003;
    public const int ReportFoundSuccessfully = 3004;
    public const int ReportsListedSuccessfully = 3005;
    public const int ReportExportedSuccessfully = 3006;
    public const int ReportScheduledSuccessfully = 3007;

    // Validation Errors (3100-3199)
    public const int ReportNameRequired = 3100;
    public const int InvalidReportId = 3101;
    public const int InvalidReportType = 3102;
    public const int InvalidDateRange = 3103;
    public const int ReportParametersRequired = 3104;
    public const int InvalidExportFormat = 3105;
    public const int InvalidReportFilter = 3106;
    public const int ReportTemplateRequired = 3107;

    // Business Logic Errors (3200-3299)
    public const int ReportNotFound = 3200;
    public const int ReportAlreadyExists = 3201;
    public const int ReportGenerationInProgress = 3202;
    public const int ReportDataNotAvailable = 3203;
    public const int ReportSizeLimitExceeded = 3204;
    public const int ReportGenerationFailed = 3205;
    public const int InsufficientDataForReport = 3206;
    public const int ReportExpired = 3207;

    // System Errors (3300-3399)
    public const int DatabaseConnectionError = 3300;
    public const int DatabaseOperationFailed = 3301;
    public const int FileSystemError = 3302;
    public const int ReportEngineError = 3303;
    public const int UnexpectedError = 3304;
    public const int ExportServiceUnavailable = 3305;
    public const int MemoryLimitExceeded = 3306;

    public static Dictionary<int, string> Messages = new()
    {
        // Success Messages
        { ReportGeneratedSuccessfully, "Rapor başarıyla oluşturuldu" },
        { ReportCreatedSuccessfully, "Rapor başarıyla yaratıldı" },
        { ReportUpdatedSuccessfully, "Rapor başarıyla güncellendi" },
        { ReportDeletedSuccessfully, "Rapor başarıyla silindi" },
        { ReportFoundSuccessfully, "Rapor başarıyla bulundu" },
        { ReportsListedSuccessfully, "Raporlar başarıyla listelendi" },
        { ReportExportedSuccessfully, "Rapor başarıyla dışa aktarıldı" },
        { ReportScheduledSuccessfully, "Rapor başarıyla zamanlandı" },

        // Validation Errors
        { ReportNameRequired, "Rapor adı zorunludur" },
        { InvalidReportId, "Geçersiz rapor kimliği" },
        { InvalidReportType, "Geçersiz rapor türü" },
        { InvalidDateRange, "Geçersiz tarih aralığı" },
        { ReportParametersRequired, "Rapor parametreleri zorunludur" },
        { InvalidExportFormat, "Geçersiz dışa aktarma formatı" },
        { InvalidReportFilter, "Geçersiz rapor filtresi" },
        { ReportTemplateRequired, "Rapor şablonu zorunludur" },

        // Business Logic Errors
        { ReportNotFound, "Rapor bulunamadı" },
        { ReportAlreadyExists, "Rapor zaten mevcut" },
        { ReportGenerationInProgress, "Rapor oluşturma işlemi zaten devam ediyor" },
        { ReportDataNotAvailable, "Rapor verisi mevcut değil" },
        { ReportSizeLimitExceeded, "Rapor boyut sınırı aşıldı" },
        { ReportGenerationFailed, "Rapor oluşturma başarısız" },
        { InsufficientDataForReport, "Rapor oluşturmak için yetersiz veri" },
        { ReportExpired, "Raporun süresi dolmuş" },

        // System Errors
        { DatabaseConnectionError, "Veritabanı bağlantı hatası" },
        { DatabaseOperationFailed, "Veritabanı işlemi başarısız" },
        { FileSystemError, "Dosya sistemi hatası" },
        { ReportEngineError, "Rapor motoru hatası" },
        { UnexpectedError, "Beklenmeyen bir hata oluştu" },
        { ExportServiceUnavailable, "Dışa aktarma servisi kullanılamıyor" },
        { MemoryLimitExceeded, "Rapor oluşturma sırasında bellek sınırı aşıldı" }
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
