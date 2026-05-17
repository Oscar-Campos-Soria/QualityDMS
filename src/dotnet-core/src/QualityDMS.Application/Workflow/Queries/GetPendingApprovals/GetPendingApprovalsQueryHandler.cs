using MediatR;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Application.Workflow.Queries.GetPendingApprovals;

public class GetPendingApprovalsQueryHandler(IWorkflowRepository workflowRepository)
    : IRequestHandler<GetPendingApprovalsQuery, IEnumerable<PendingApprovalDto>>
{
    public async Task<IEnumerable<PendingApprovalDto>> Handle(GetPendingApprovalsQuery query, CancellationToken ct)
    {
        var instances = await workflowRepository.GetPendingByUserAsync(query.UserId, ct);

        return instances.Select(i =>
        {
            var step = i.WorkflowTemplate?.Steps.FirstOrDefault(s => s.StepOrder == i.CurrentStepOrder);
            return new PendingApprovalDto(
                i.DocumentId,
                i.Document?.Code ?? string.Empty,
                i.Document?.Title ?? string.Empty,
                i.CurrentStepOrder,
                step?.StepName ?? $"Paso {i.CurrentStepOrder}",
                i.CreatedAt
            );
        });
    }
}
