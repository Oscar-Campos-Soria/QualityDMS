using CalidadSYS.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityDMS.Application.Workflow.Commands.ApproveStep;
using QualityDMS.Application.Workflow.Commands.RejectStep;
using QualityDMS.Application.Workflow.Queries.GetPendingApprovals;
using QualityDMS.Domain.Interfaces;

namespace CalidadSYS.Controllers;

[Authorize]
public class ApprovalsController(IMediator mediator, ICurrentUserService currentUser) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var pending = await mediator.Send(new GetPendingApprovalsQuery(currentUser.UserId), ct);
        ViewData["Title"] = "Aprobaciones Pendientes";
        ViewBag.PendingCount = pending.Count();
        return View(pending);
    }

    [Authorize(Policy = "CanApproveDocuments")]
    public async Task<IActionResult> Review(int documentId, CancellationToken ct)
    {
        var pending = await mediator.Send(new GetPendingApprovalsQuery(currentUser.UserId), ct);
        var item = pending.FirstOrDefault(p => p.DocumentId == documentId);
        if (item is null) return NotFound();

        ViewData["Title"] = $"Revisar: {item.DocumentCode}";
        return View(new ReviewApprovalViewModel
        {
            DocumentId = item.DocumentId,
            DocumentCode = item.DocumentCode,
            DocumentTitle = item.DocumentTitle,
            CurrentStep = item.CurrentStep,
            StepName = item.StepName,
            SubmittedAt = item.SubmittedAt
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CanApproveDocuments")]
    public async Task<IActionResult> Review(ReviewApprovalViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        if (vm.Action == "Approve")
        {
            var result = await mediator.Send(new ApproveStepCommand(vm.DocumentId, vm.Comments), ct);
            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess ? $"Documento {vm.DocumentCode} aprobado." : result.Error;
        }
        else if (vm.Action == "Reject")
        {
            if (string.IsNullOrWhiteSpace(vm.RejectReason))
            {
                ModelState.AddModelError(nameof(vm.RejectReason), "El motivo de rechazo es requerido.");
                return View(vm);
            }

            var result = await mediator.Send(new RejectStepCommand(vm.DocumentId, vm.RejectReason), ct);
            TempData[result.IsSuccess ? "Success" : "Error"] =
                result.IsSuccess ? $"Documento {vm.DocumentCode} rechazado." : result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
