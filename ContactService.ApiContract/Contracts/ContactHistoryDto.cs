namespace ContactService.ApiContract.Contracts
{
    public class ContactHistoryDto
    {
        public Guid Id { get; set; }
        public Guid ContactId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? AdditionalMetadata { get; set; }
    }

    public class ContactHistoryListDto
    {
        public IEnumerable<ContactHistoryDto> Histories { get; set; } = new List<ContactHistoryDto>();
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
    }

    public class ContactReplayResultDto
    {
        public Guid ContactId { get; set; }
        public DateTime? PointInTime { get; set; }
        public ContactDto? ReplayedContact { get; set; }
        public bool Exists { get; set; }
        public string Status { get; set; } = string.Empty; // EXISTS, DELETED, NOT_FOUND
    }
}