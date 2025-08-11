using ContactService.ApiContract.Contracts;
using MediatR;
using Shared.CrossCutting;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ContactService.ApiContract.Request.Queries
{
    public class GetContactHistoryQuery : IQuery<PageResponse<ContactHistoryDto>>
    {
        public Guid ContactId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetHistoryByCorrelationIdQuery : IQuery<ContactHistoryListDto>
    {
        public string CorrelationId { get; set; } = string.Empty;
    }

    public class GetHistoryByOperationTypeQuery : IQuery<PageResponse<ContactHistoryDto>>
    {
        public string OperationType { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class GetHistoryByDateRangeQuery : IQuery<PageResponse<ContactHistoryDto>>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class ReplayContactStateQuery : IQuery<ContactReplayResultDto>
    {
        public Guid ContactId { get; set; }
        public DateTime? PointInTime { get; set; }
    }
}