using QualityDMS.Domain.Common;
using QualityDMS.Domain.Entities;

namespace QualityDMS.Domain.Events;

public class DocumentObsoletedEvent(Document document) : DomainEvent
{
    public Document Document { get; } = document;
}
