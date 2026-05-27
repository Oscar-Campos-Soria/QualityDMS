using QualityDMS.Domain.Entities;

namespace QualityDMS.Domain.Interfaces;

public interface IWorkflowRepository
{
    Task<WorkflowInstance?> GetActiveInstanceByDocumentAsync(int documentId, CancellationToken ct = default);
    Task<WorkflowTemplate?> GetTemplateByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<WorkflowInstance>> GetPendingByUserAsync(string userId, CancellationToken ct = default);
    Task AddInstanceAsync(WorkflowInstance instance, CancellationToken ct = default);
    void UpdateInstance(WorkflowInstance instance);
    Task AddActionAsync(WorkflowAction action, CancellationToken ct = default);
}
