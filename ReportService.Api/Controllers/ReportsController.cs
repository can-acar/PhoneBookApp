using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReportService.ApiContract.Request.Commands;
using ReportService.ApiContract.Request.Queries;

namespace ReportService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetReportById), new { id = result.Data?.ReportId }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllReports()
    {
        var query = new GetAllReportsQuery();
        var reports = await _mediator.Send(query);
        return Ok(reports);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReportById(Guid id)
    {
        var query = new GetReportByIdQuery { Id = id };
        var response = await _mediator.Send(query);

        if (response == null || !response.Success || response.Data == null)
            return NotFound();

        return Ok(response);
    }
}