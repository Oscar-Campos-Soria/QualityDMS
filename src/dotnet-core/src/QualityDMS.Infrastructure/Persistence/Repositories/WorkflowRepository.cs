using Microsoft.EntityFrameworkCore;
using QualityDMS.Domain.Entities;
using QualityDMS.Domain.Enums;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Infrastructure.Persistence.Repositories;

public class WorkflowRepository(QualityDMSDbContext ctx) : IWorkflowRepository
{
    public async Task<WorkflowInstance?> GetActiveInstanceByDocumentAsync(int documentId, CancellationToken ct = default)
        => await ctx.WorkflowInstances
            .Include(i => i.Actions)
            .FirstOrDefaultAsync(i => i.DocumentId == documentId
                && i.Status == WorkflowStepStatus.InProgress, ct);

    public async Task<WorkflowTemplate?> GetTemplateByIdAsync(int id, CancellationToken ct = default)
        => await ctx.WorkflowTemplates
            .Include(t => t.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(t => t.WorkflowTemplateId == id, ct);

    public async Task<IEnumerable<WorkflowInstance>> GetPendingByUserAsync(string userId, CancellationToken ct = default)
    {
        return await ctx.WorkflowInstances
            .AsNoTracking()
            .Include(i => i.Document)
            .Include(i => i.WorkflowTemplate)
                .ThenInclude(t => t.Steps)
            .Where(i => i.Status == WorkflowStepStatus.InProgress)
            .Where(i => i.WorkflowTemplate.Steps
                .Any(s => s.StepOrder == i.CurrentStepOrder
                    && (s.AssignedUserId == userId || s.AssignedRoleName != null)))
            .ToListAsync(ct);
    }

    public async Task AddInstanceAsync(WorkflowInstance instance, CancellationToken ct = default)
        => await ctx.WorkflowInstances.AddAsync(instance, ct);

    public void UpdateInstance(WorkflowInstance instance) => ctx.WorkflowInstances.Update(instance);

    public async Task AddActionAsync(WorkflowAction action, CancellationToken ct = default)
        => await ctx.WorkflowActions.AddAsync(action, ct);
}
