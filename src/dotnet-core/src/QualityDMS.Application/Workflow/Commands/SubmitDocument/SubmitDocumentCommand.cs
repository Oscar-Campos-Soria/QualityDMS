using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Workflow.Commands.SubmitDocument;

public record SubmitDocumentCommand(int DocumentId) : IRequest<Result>;
