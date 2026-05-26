using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityDMS.Application.Documents.Commands.CreateDocument;
using QualityDMS.Application.Documents.Commands.ObsoleteDocument;
using QualityDMS.Application.Documents.Commands.UpdateDocument;
using QualityDMS.Application.Documents.Queries.GetDocumentById;
using QualityDMS.Application.Documents.Queries.GetDocuments;
using QualityDMS.Application.Documents.Queries.GetExpiringDocuments;
using QualityDMS.Domain.Enums;

namespace CalidadSYS.Controllers.Api;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "DocumentManager,QualityManager,Admin")]
    public async Task<IActionResult> Create([FromForm] CreateDocumentRequest req, CancellationToken ct)
    {
        if (req.File is null) return BadRequest("Archivo requerido.");

        var cmd = new CreateDocumentCommand(
            req.Code, req.Title, req.Description,
            req.CategoryId, req.DepartmentId, req.WorkflowTemplateId,
            req.NextReviewDate, req.File.FileName, req.File.ContentType,
            req.File.Length, req.File.OpenReadStream());

        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value })
            : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? categoryId, [FromQuery] int? departmentId,
        [FromQuery] DocumentStatus? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetDocumentsQuery(categoryId, departmentId, status, search, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetDocumentByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "DocumentManager,QualityManager,Admin")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateDocumentRequest req, CancellationToken ct)
    {
        var cmd = new UpdateDocumentCommand(
            id, req.Title, req.Description, req.CategoryId, req.DepartmentId,
            req.WorkflowTemplateId, req.NextReviewDate, req.NewVersionNumber,
            req.File?.FileName, req.File?.ContentType, req.File?.Length,
            req.File?.OpenReadStream(), req.ChangeLog);

        var result = await mediator.Send(cmd, ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{id:int}/obsolete")]
    [Authorize(Roles = "QualityManager,Admin")]
    public async Task<IActionResult> Obsolete(int id, CancellationToken ct)
    {
        var result = await mediator.Send(new ObsoleteDocumentCommand(id), ct);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiring([FromQuery] int days = 30, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetExpiringDocumentsQuery(days), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}/download/{versionId:int}")]
    public async Task<IActionResult> Download(int id, int versionId, CancellationToken ct)
    {
        // TODO: implement DownloadDocumentQuery
        return StatusCode(501, "Not implemented yet");
    }
}

public class CreateDocumentRequest
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int DepartmentId { get; set; }
    public int? WorkflowTemplateId { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public IFormFile? File { get; set; }
}

public class UpdateDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int DepartmentId { get; set; }
    public int? WorkflowTemplateId { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public string? NewVersionNumber { get; set; }
    public string? ChangeLog { get; set; }
    public IFormFile? File { get; set; }
}
