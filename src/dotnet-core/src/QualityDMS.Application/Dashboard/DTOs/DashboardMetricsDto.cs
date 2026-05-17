namespace QualityDMS.Application.Dashboard.DTOs;

public class DashboardMetricsDto
{
    public int TotalDocuments { get; init; }
    public int ApprovedDocuments { get; init; }
    public int PendingApprovals { get; init; }
    public int DraftDocuments { get; init; }
    public int ObsoleteDocuments { get; init; }
    public int DocumentsForReview { get; init; }
    public double ComplianceRate { get; init; }
    public IEnumerable<DocumentsByStatusDto> DocumentsByStatus { get; init; } = [];
    public IEnumerable<RecentActivityDto> RecentActivity { get; init; } = [];
}

public record DocumentsByStatusDto(string Status, int Count);
public record RecentActivityDto(string DocumentCode, string Title, string Action, DateTime Date, string User);
