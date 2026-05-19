namespace QualityDMS.Domain.Interfaces;

public interface IPublicDmsWebhookService
{
    Task NotifyDocumentApprovedAsync(int documentId);
}
