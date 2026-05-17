using QualityDMS.Domain.Common;
using QualityDMS.Domain.Enums;
using QualityDMS.Domain.Events;

namespace QualityDMS.Domain.Entities;

public class Document : AuditableEntity
{
    private readonly List<DocumentVersion> _versions = new();
    private readonly List<DomainEvent> _domainEvents = new();

    public int DocumentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;
    public int CategoryId { get; set; }
    public int DepartmentId { get; set; }
    public int? WorkflowTemplateId { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime? NextReviewDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DocumentCategory Category { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public WorkflowTemplate? WorkflowTemplate { get; set; }
    public IReadOnlyCollection<DocumentVersion> Versions => _versions.AsReadOnly();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public ICollection<WorkflowInstance> WorkflowInstances { get; set; } = new List<WorkflowInstance>();
    public ICollection<ControlledDistribution> Distributions { get; set; } = new List<ControlledDistribution>();

    public static Document Create(string code, string title, int categoryId, int departmentId, string createdBy)
    {
        var doc = new Document
        {
            Code = code,
            Title = title,
            CategoryId = categoryId,
            DepartmentId = departmentId,
            Status = DocumentStatus.Draft,
            CreatedBy = createdBy
        };
        doc._domainEvents.Add(new DocumentCreatedEvent(doc));
        return doc;
    }

    public void AddVersion(DocumentVersion version)
    {
        _versions.Add(version);
    }

    public void SubmitForApproval()
    {
        if (Status != DocumentStatus.Draft && Status != DocumentStatus.Rejected)
            throw new InvalidOperationException("Solo documentos en borrador o rechazados pueden enviarse a aprobación.");
        Status = DocumentStatus.PendingApproval;
        _domainEvents.Add(new DocumentSubmittedEvent(this));
    }

    public void Approve(string approvedBy)
    {
        if (Status != DocumentStatus.PendingApproval)
            throw new InvalidOperationException("Documento no está en revisión.");
        Status = DocumentStatus.Approved;
        EffectiveDate = DateTime.UtcNow;
        _domainEvents.Add(new DocumentApprovedEvent(this, approvedBy));
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status != DocumentStatus.PendingApproval)
            throw new InvalidOperationException("Documento no está en revisión.");
        Status = DocumentStatus.Rejected;
        _domainEvents.Add(new DocumentRejectedEvent(this, rejectedBy, reason));
    }

    public void Obsolete()
    {
        Status = DocumentStatus.Obsolete;
        IsActive = false;
        _domainEvents.Add(new DocumentObsoletedEvent(this));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
