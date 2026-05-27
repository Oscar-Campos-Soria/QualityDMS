using MediatR;
using QualityDMS.Application.Dashboard.DTOs;

namespace QualityDMS.Application.Dashboard.Queries.GetMetrics;

public record GetDashboardMetricsQuery : IRequest<DashboardMetricsDto>;
