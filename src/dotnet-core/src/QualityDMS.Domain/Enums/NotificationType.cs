namespace QualityDMS.Domain.Enums;

public enum NotificationType
{
    DocumentPendingApproval = 1,
    DocumentApproved = 2,
    DocumentRejected = 3,
    DocumentExpiring = 4,
    DocumentObsoleted = 5,
    AuditScheduled = 6,
    WorkflowStepAssigned = 7
}
