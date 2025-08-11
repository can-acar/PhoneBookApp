namespace NotificationService.Domain.Entities
{
    public class NotificationTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SubjectTemplate { get; set; } = string.Empty;
        public string ContentTemplate { get; set; } = string.Empty;
        public string? Language { get; set; }
        public string TemplateType { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
