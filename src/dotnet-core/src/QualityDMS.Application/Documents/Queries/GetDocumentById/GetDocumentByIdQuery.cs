using MediatR;
using QualityDMS.Application.Documents.DTOs;

namespace QualityDMS.Application.Documents.Queries.GetDocumentById;

public record GetDocumentByIdQuery(int DocumentId) : IRequest<DocumentDetailDto?>;
