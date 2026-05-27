using MediatR;

namespace QualityDMS.Application.Workflow.Queries.GetPendingApprovals;

public record PendingApprovalDto(
    int DocumentId,
    string DocumentCode,
    string DocumentTitle,
    int CurrentStep,
    string StepName,
    DateTime SubmittedAt
);

public record GetPendingApprovalsQuery(string UserId) : IRequest<IEnumerable<PendingApprovalDto>>;
