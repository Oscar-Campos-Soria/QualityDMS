using Microsoft.Extensions.Logging;
using QualityDMS.Domain.Interfaces;
using System.Net.Http.Json;

namespace QualityDMS.Infrastructure.Services;

public class PublicDmsWebhookService(
    HttpClient httpClient,
    ILogger<PublicDmsWebhookService> logger) : IPublicDmsWebhookService
{
    public async Task NotifyDocumentApprovedAsync(int documentId)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/indexer/notify",
                new { document_id = documentId });

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("Webhook notify [{Status}] document {Id}",
                    response.StatusCode, documentId);
            else
                logger.LogInformation("Webhook notified: document {Id} queued for indexing", documentId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook notify failed for document {Id}", documentId);
        }
    }
}
