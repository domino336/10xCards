using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Admin;

namespace _10xCards.Application.Services;

public interface IAdminService
{
    Task<Result<AdminMetricsDto>> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default);
}
