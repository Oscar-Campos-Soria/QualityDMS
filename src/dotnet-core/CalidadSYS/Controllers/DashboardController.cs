using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityDMS.Application.Dashboard.Queries.GetMetrics;
using QualityDMS.Application.Workflow.Queries.GetPendingApprovals;
using QualityDMS.Domain.Interfaces;

namespace CalidadSYS.Controllers;

[Authorize]
public class DashboardController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var metrics = await mediator.Send(new GetDashboardMetricsQuery(), ct);
        var pending = await mediator.Send(new GetPendingApprovalsQuery(currentUser.UserId), ct);

        ViewBag.PendingCount = pending.Count();
        ViewData["Title"] = "Dashboard";

        return View(metrics);
    }
}
