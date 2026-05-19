namespace QualityDMS.Domain.Interfaces;

public interface IPhpSyncService
{
    Task TriggerSyncAsync();
}
