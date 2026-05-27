using MediatR;
using QualityDMS.Domain.Common;

namespace QualityDMS.Application.Workflow.Commands.ApproveStep;

public record ApproveStepCommand(int DocumentId, string? Comments) : IRequest<Result>;
