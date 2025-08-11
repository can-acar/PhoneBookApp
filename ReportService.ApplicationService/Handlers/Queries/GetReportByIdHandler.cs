using MediatR;
using ReportService.ApiContract.Contracts;
using ReportService.ApiContract.Request.Queries;
using ReportService.Domain.Interfaces;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ReportService.ApplicationService.Handlers.Queries;

public class GetReportByIdHandler : IQueryHandler<GetReportByIdQuery, ReportDto>
{
    private readonly IReportService _reportService;

    public GetReportByIdHandler(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<ApiResponse<ReportDto>> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var report = await _reportService.GetByIdAsync(request.Id, cancellationToken);

            if (report == null)
            {
                return ApiResponse.Result<ReportDto>(
                    false,
                    null,
                    404,
                    "Report not found");
            }

            var result = new ReportDto
            {
                Id = report.Id,
                RequestedAt = report.RequestedAt,
                Status = report.Status.ToString(),
                CompletedAt = report.CompletedAt,
                LocationStatistics = report.LocationStatistics.Select(ls => new LocationStatisticDto
                {
                    Location = ls.Location,
                    PersonCount = ls.PersonCount,
                    PhoneNumberCount = ls.PhoneNumberCount
                }).ToList()
            };

            return ApiResponse.Result<ReportDto>(
                true,
                result,
                200,
                "Report retrieved successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse.Result<ReportDto>(
                false,
                null,
                500,
                "An error occurred while retrieving the report");
        }
    }
}