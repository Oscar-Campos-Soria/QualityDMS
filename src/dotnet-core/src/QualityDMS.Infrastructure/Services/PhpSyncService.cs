using Microsoft.Extensions.Logging;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Infrastructure.Services;

public class PhpSyncService(
    HttpClient httpClient,
    ILogger<PhpSyncService> logger) : IPhpSyncService
{
    public async Task TriggerSyncAsync()
    {
        try
        {
            var response = await httpClient.PostAsync("/sync/trigger_sync.php", null);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning("PHP sync trigger failed [{Status}]", response.StatusCode);
            else
                logger.LogInformation("PHP sync triggered successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PHP sync trigger error");
        }
    }
}
