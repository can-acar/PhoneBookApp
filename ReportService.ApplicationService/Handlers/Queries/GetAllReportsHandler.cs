using MediatR;
using ReportService.ApiContract.Contracts;
using ReportService.ApiContract.Request.Queries;
using ReportService.Domain.Interfaces;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ReportService.ApplicationService.Handlers.Queries;

public class GetAllReportsHandler : IQueryHandler<GetAllReportsQuery, List<ReportDto>>
{
    private readonly IReportService _reportService;

    public GetAllReportsHandler(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<ApiResponse<List<ReportDto>>> Handle(GetAllReportsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var reports = await _reportService.GetAllAsync(cancellationToken);

            var result = reports.Select(r => new ReportDto
            {
                Id = r.Id,
                RequestedAt = r.RequestedAt,
                Status = r.Status.ToString(),
                CompletedAt = r.CompletedAt,
                LocationStatistics = r.LocationStatistics.Select(ls => new LocationStatisticDto
                {
                    Location = ls.Location,
                    PersonCount = ls.PersonCount,
                    PhoneNumberCount = ls.PhoneNumberCount
                }).ToList()
            }).ToList();

            return ApiResponse.Result<List<ReportDto>>(
                true,
                result,
                200,
                "Reports retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.Result<List<ReportDto>>(
                false,
                null,
                500,
                "An error occurred while retrieving reports");
        }
    }
}