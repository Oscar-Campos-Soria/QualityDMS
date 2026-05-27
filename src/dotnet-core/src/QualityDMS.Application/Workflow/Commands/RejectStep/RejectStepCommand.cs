using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Workflow.Commands.RejectStep;

public record RejectStepCommand(int DocumentId, string Reason) : IRequest<Result>;
