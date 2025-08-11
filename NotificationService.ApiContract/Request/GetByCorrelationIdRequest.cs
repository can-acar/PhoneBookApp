namespace NotificationService.ApiContract.Request
{
    public class GetByCorrelationIdRequest
    {
        public string CorrelationId { get; set; } = string.Empty;
    }
}
