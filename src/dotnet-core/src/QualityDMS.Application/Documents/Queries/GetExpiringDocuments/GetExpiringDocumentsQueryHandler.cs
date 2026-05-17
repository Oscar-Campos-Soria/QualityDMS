using MediatR;
using QualityDMS.Application.Documents.DTOs;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Application.Documents.Queries.GetExpiringDocuments;

public class GetExpiringDocumentsQueryHandler(IDocumentRepository documentRepository)
    : IRequestHandler<GetExpiringDocumentsQuery, IEnumerable<DocumentDto>>
{
    public async Task<IEnumerable<DocumentDto>> Handle(GetExpiringDocumentsQuery query, CancellationToken ct)
    {
        var docs = await documentRepository.GetExpiringAsync(query.DaysAhead, ct);

        return docs.Select(d => new DocumentDto
        {
            DocumentId = d.DocumentId,
            Code = d.Code,
            Title = d.Title,
            Status = d.Status,
            CategoryName = d.Category?.Name ?? string.Empty,
            DepartmentName = d.Department?.Name ?? string.Empty,
            NextReviewDate = d.NextReviewDate,
            CreatedAt = d.CreatedAt,
            CreatedBy = d.CreatedBy
        });
    }
}
