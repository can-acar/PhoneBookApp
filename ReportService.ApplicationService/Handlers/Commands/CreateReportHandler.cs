using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReportService.ApiContract.Request.Commands;
using ReportService.ApiContract.Response.Commands;
using ReportService.Domain.Entities;
using ReportService.Domain.Enums;
using ReportService.Domain.Interfaces;
using ReportService.Domain.Events;
using Shared.CrossCutting.Interfaces;
using Shared.CrossCutting.Models;

namespace ReportService.ApplicationService.Handlers.Commands
{
    public class CreateReportHandler : ICommandHandler<CreateReportCommand, CreateReportResponse>
    {
        private readonly IReportRepository _reportRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateReportHandler> _logger;

        public CreateReportHandler(
            IReportRepository reportRepository,
            IEventPublisher eventPublisher,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateReportHandler> logger)
        {
            _reportRepository = reportRepository;
            _eventPublisher = eventPublisher;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ApiResponse<CreateReportResponse>> Handle(CreateReportCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new report for location: {Location}", request.Location);

                // Use rich domain constructor
                var report = new Report(
                    location: request.Location ?? string.Empty,
                    requestedBy: request.RequestedBy ?? "System"
                );

                await _reportRepository.CreateAsync(report, cancellationToken);

                // Get correlation ID from HTTP context if available
                string correlationId = Guid.NewGuid().ToString();
                if (_httpContextAccessor.HttpContext != null && 
                    _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var values))
                {
                    correlationId = values.FirstOrDefault() ?? correlationId;
                }

                // Publish event to Kafka for asynchronous processing
                var reportRequestedEvent = new ReportRequestedEvent
                {
                    ReportId = report.Id,
                    Location = report.Location,
                    RequestedBy = report.RequestedBy,
                    RequestedAt = report.RequestedAt,
                    CorrelationId = correlationId,
                    Metadata = new Dictionary<string, object>
                    {
                        ["source"] = "API",
                        ["userAgent"] = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown"
                    }
                };

                await _eventPublisher.PublishAsync("report-events", reportRequestedEvent, cancellationToken);

                _logger.LogInformation("Report {ReportId} created and queued for processing", report.Id);

                var response = new CreateReportResponse
                {
                    ReportId = report.Id,
                    Status = report.Status.ToString(),
                    Message = "Rapor talebi alındı ve işleme kuyruğuna eklendi"
                };

                return ApiResponse.Result<CreateReportResponse>(
                    true, 
                    response, 
                    200, 
                    "Report creation request received and queued for processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating report for location: {Location}", request.Location);
                
                return ApiResponse.Result<CreateReportResponse>(
                    false, 
                    null, 
                    500, 
                    "An error occurred while creating the report");
            }
        }
    }
}