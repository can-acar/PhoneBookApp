namespace ContactService.Domain.Enums;

public enum OutboxEventStatus
{
    Pending,
    Processed,
    Failed
}