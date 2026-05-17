using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Documents.Commands.ObsoleteDocument;

public record ObsoleteDocumentCommand(int DocumentId) : IRequest<Result>;
