using MediatR;
using QualityDMS.Application.Common.Models;
using QualityDMS.Application.Documents.DTOs;
using QualityDMS.Domain.Enums;

namespace QualityDMS.Application.Documents.Queries.GetDocuments;

public record GetDocumentsQuery(
    int? CategoryId,
    int? DepartmentId,
    DocumentStatus? Status,
    string? SearchTerm,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<DocumentDto>>;
