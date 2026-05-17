using QualityDMS.Domain.Common;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Domain.Events;

public class DocumentCreatedEvent(Document document) : DomainEvent
{
    public Document Document { get; } = document;
}

public class DocumentSubmittedEvent(Document document) : DomainEvent
{
    public Document Document { get; } = document;
}
