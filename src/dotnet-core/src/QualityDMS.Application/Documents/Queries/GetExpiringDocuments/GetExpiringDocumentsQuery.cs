using MediatR;
using QualityDMS.Application.Documents.DTOs;

namespace QualityDMS.Application.Documents.Queries.GetExpiringDocuments;

public record GetExpiringDocumentsQuery(int DaysAhead = 30) : IRequest<IEnumerable<DocumentDto>>;
