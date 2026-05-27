using MediatR;
using QualityDMS.Application.Dashboard.DTOs;
using QualityDMS.Domain.Enums;
using QualityDMS.Domain.Interfaces;

namespace QualityDMS.Application.Dashboard.Queries.GetMetrics;

public class GetDashboardMetricsQueryHandler(IDocumentRepository documentRepository)
    : IRequestHandler<GetDashboardMetricsQuery, DashboardMetricsDto>
{
    public async Task<DashboardMetricsDto> Handle(GetDashboardMetricsQuery query, CancellationToken ct)
    {
        var allDocs = (await documentRepository.GetAllAsync(ct)).ToList();
        var expiring = (await documentRepository.GetExpiringAsync(30, ct)).ToList();

        var total = allDocs.Count;
        var approved = allDocs.Count(d => d.Status == DocumentStatus.Approved);
        var pending = allDocs.Count(d => d.Status == DocumentStatus.PendingApproval);
        var draft = allDocs.Count(d => d.Status == DocumentStatus.Draft);
        var obsolete = allDocs.Count(d => d.Status == DocumentStatus.Obsolete);

        var compliance = total > 0 ? Math.Round((double)approved / total * 100, 1) : 0;

        var byStatus = allDocs
            .GroupBy(d => d.Status)
            .Select(g => new DocumentsByStatusDto(g.Key.ToString(), g.Count()));

        var recent = allDocs
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .Take(10)
            .Select(d => new RecentActivityDto(
                d.Code, d.Title, d.Status.ToString(),
                d.UpdatedAt ?? d.CreatedAt, d.UpdatedBy ?? d.CreatedBy));

        return new DashboardMetricsDto
        {
            TotalDocuments = total,
            ApprovedDocuments = approved,
            PendingApprovals = pending,
            DraftDocuments = draft,
            ObsoleteDocuments = obsolete,
            DocumentsForReview = expiring.Count,
            ComplianceRate = compliance,
            DocumentsByStatus = byStatus,
            RecentActivity = recent
        };
    }
}
