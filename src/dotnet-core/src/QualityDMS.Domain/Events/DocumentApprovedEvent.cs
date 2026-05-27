using QualityDMS.Domain.Common;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Domain.Events;

public class DocumentApprovedEvent(Document document, string approvedBy) : DomainEvent
{
    public Document Document { get; } = document;
    public string ApprovedBy { get; } = approvedBy;
}

public class DocumentRejectedEvent(Document document, string rejectedBy, string reason) : DomainEvent
{
    public Document Document { get; } = document;
    public string RejectedBy { get; } = rejectedBy;
    public string Reason { get; } = reason;
}
