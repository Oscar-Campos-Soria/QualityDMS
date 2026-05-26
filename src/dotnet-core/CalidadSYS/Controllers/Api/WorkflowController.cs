using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityDMS.Application.Workflow.Commands.ApproveStep;
using QualityDMS.Application.Workflow.Commands.RejectStep;
using QualityDMS.Application.Workflow.Commands.SubmitDocument;
using QualityDMS.Application.Workflow.Queries.GetPendingApprovals;
using QualityDMS.Domain.Interfaces;

namespace CalidadSYS.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class WorkflowController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("submit/{documentId:int}")]
    public async Task<IActionResult> Submit(int documentId, CancellationToken ct)
    {
        var result = await mediator.Send(new SubmitDocumentCommand(documentId), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("approve")]
    [Authorize(Roles = "Approver,Manager,QualityManager,Admin")]
    public async Task<IActionResult> Approve([FromBody] ApproveRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new ApproveStepCommand(req.DocumentId, req.Comments), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpPost("reject")]
    [Authorize(Roles = "Approver,Manager,QualityManager,Admin")]
    public async Task<IActionResult> Reject([FromBody] RejectRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(new RejectStepCommand(req.DocumentId, req.Reason), ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPendingApprovalsQuery(currentUser.UserId), ct);
        return Ok(result);
    }
}

public record ApproveRequest(int DocumentId, string? Comments);
public record RejectRequest(int DocumentId, string Reason);
