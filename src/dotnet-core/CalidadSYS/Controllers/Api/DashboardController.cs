using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityDMS.Application.Dashboard.Queries.GetMetrics;

namespace CalidadSYS.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet("metrics")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetMetrics(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDashboardMetricsQuery(), ct);
        return Ok(result);
    }
}
