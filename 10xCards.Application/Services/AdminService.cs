using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Admin;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;

namespace _10xCards.Application.Services;

public sealed class AdminService : IAdminService
{
    private readonly CardsDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "AdminMetrics";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public AdminService(CardsDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<AdminMetricsDto>> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(CacheKey, out AdminMetricsDto? cachedMetrics) && cachedMetrics != null)
            {
                return Result<AdminMetricsDto>.Success(cachedMetrics);
            }

            // Calculate metrics
            var now = DateTime.UtcNow;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.AddDays(-30);

            // Total cards
            var totalCards = await _db.Cards.CountAsync(cancellationToken);
            var aiCards = await _db.Cards.CountAsync(c => c.GenerationMethod == GenerationMethod.Ai, cancellationToken);
            var manualCards = totalCards - aiCards;

            // AI Acceptance Rate (users with at least 5 cards)
            var eligibleUserIds = await _db.Cards
                .GroupBy(c => c.UserId)
                .Where(g => g.Count() >= 5)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken);

            double aiAcceptanceRate = 0;
            if (eligibleUserIds.Any())
            {
                var totalAcceptanceEvents = await _db.AcceptanceEvents
                    .Where(e => eligibleUserIds.Contains(e.UserId))
                    .CountAsync(cancellationToken);

                if (totalAcceptanceEvents > 0)
                {
                    var acceptedCount = await _db.AcceptanceEvents
                        .Where(e => eligibleUserIds.Contains(e.UserId) && e.Action == AcceptanceAction.Accepted)
                        .CountAsync(cancellationToken);

                    aiAcceptanceRate = (double)acceptedCount / totalAcceptanceEvents;
                }
            }

            // Percentiles - calculate per-user acceptance rates
            var userAcceptanceRates = new List<double>();
            foreach (var userId in eligibleUserIds)
            {
                var userTotal = await _db.AcceptanceEvents
                    .CountAsync(e => e.UserId == userId, cancellationToken);

                if (userTotal > 0)
                {
                    var userAccepted = await _db.AcceptanceEvents
                        .CountAsync(e => e.UserId == userId && e.Action == AcceptanceAction.Accepted, cancellationToken);

                    userAcceptanceRates.Add((double)userAccepted / userTotal);
                }
            }

            var percentiles = CalculatePercentiles(userAcceptanceRates);

            // Active users
            var activeUsers7d = await _db.Cards
                .Where(c => c.CreatedUtc >= sevenDaysAgo)
                .Select(c => c.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var activeUsers30d = await _db.Cards
                .Where(c => c.CreatedUtc >= thirtyDaysAgo)
                .Select(c => c.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            // Generation errors
            var generationErrors = await _db.GenerationErrors
                .CountAsync(e => e.CreatedUtc >= sevenDaysAgo, cancellationToken);

            var metrics = new AdminMetricsDto(
                totalCards,
                aiCards,
                manualCards,
                aiAcceptanceRate,
                percentiles,
                activeUsers7d,
                activeUsers30d,
                generationErrors
            );

            // Cache the result
            _cache.Set(CacheKey, metrics, CacheDuration);

            return Result<AdminMetricsDto>.Success(metrics);
        }
        catch (Exception ex)
        {
            return Result<AdminMetricsDto>.Failure($"Failed to calculate metrics: {ex.Message}");
        }
    }

    private PercentileData CalculatePercentiles(List<double> values)
    {
        if (!values.Any())
            return new PercentileData(0, 0, 0);

        var sorted = values.OrderBy(v => v).ToList();
        var count = sorted.Count;

        return new PercentileData(
            GetPercentile(sorted, count, 0.5),
            GetPercentile(sorted, count, 0.75),
            GetPercentile(sorted, count, 0.9)
        );
    }

    private double GetPercentile(List<double> sorted, int count, double percentile)
    {
        if (count == 0)
            return 0;

        var index = (int)Math.Ceiling(count * percentile) - 1;
        index = Math.Max(0, Math.Min(index, count - 1));
        
        return sorted[index];
    }
}
