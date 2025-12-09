using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using _10xCards.Application.Common;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;

namespace _10xCards.Application.Services;

public sealed class SrService : ISrService
{
    private readonly CardsDbContext _db;

    public SrService(CardsDbContext db) => _db = db;

    public async Task<Result> RecordReviewAsync(
        Guid cardId,
        Guid userId,
        ReviewResult result,
        CancellationToken cancellationToken = default)
    {
        var progress = await _db.CardProgress
            .FirstOrDefaultAsync(p => p.CardId == cardId && p.UserId == userId, cancellationToken);

        if (progress is null)
            return Result.Failure("Card progress not found or access denied");

        var srState = DeserializeSrState(progress.SrState);
        var (newInterval, newEaseFactor, newRepetitions) = CalculateNextReview(srState, result);

        var now = DateTime.UtcNow;
        progress.ReviewCount++;
        progress.LastReviewUtc = now;
        progress.LastReviewResult = result;
        progress.NextReviewUtc = now.Add(newInterval);
        progress.SrState = SerializeSrState(newEaseFactor, newRepetitions);
        progress.UpdatedUtc = now;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private (TimeSpan Interval, double EaseFactor, int Repetitions) CalculateNextReview(
        SrState state,
        ReviewResult result)
    {
        var easeFactor = state.EaseFactor;
        var repetitions = state.Repetitions;

        switch (result)
        {
            case ReviewResult.Again:
                // Reset to beginning
                repetitions = 0;
                easeFactor = Math.Max(1.3, easeFactor - 0.2);
                return (TimeSpan.FromMinutes(10), easeFactor, repetitions);

            case ReviewResult.Good:
                repetitions++;
                easeFactor = Math.Max(1.3, easeFactor - 0.15);
                break;

            case ReviewResult.Easy:
                repetitions++;
                easeFactor = Math.Min(2.5, easeFactor + 0.15);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(result));
        }

        // SM-2 algorithm interval calculation
        var interval = repetitions switch
        {
            1 => TimeSpan.FromDays(1),
            2 => TimeSpan.FromDays(6),
            _ => TimeSpan.FromDays(Math.Ceiling(state.LastInterval.TotalDays * easeFactor))
        };

        return (interval, easeFactor, repetitions);
    }

    private SrState DeserializeSrState(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json) || json == "{}")
            {
                return new SrState
                {
                    EaseFactor = 2.5,
                    Repetitions = 0,
                    LastInterval = TimeSpan.Zero
                };
            }

            var state = JsonSerializer.Deserialize<SrState>(json);
            return state ?? new SrState
            {
                EaseFactor = 2.5,
                Repetitions = 0,
                LastInterval = TimeSpan.Zero
            };
        }
        catch
        {
            return new SrState
            {
                EaseFactor = 2.5,
                Repetitions = 0,
                LastInterval = TimeSpan.Zero
            };
        }
    }

    private string SerializeSrState(double easeFactor, int repetitions)
    {
        var state = new SrState
        {
            EaseFactor = easeFactor,
            Repetitions = repetitions,
            LastInterval = TimeSpan.FromDays(1)
        };

        return JsonSerializer.Serialize(state);
    }

    private sealed class SrState
    {
        public double EaseFactor { get; set; } = 2.5;
        public int Repetitions { get; set; }
        public TimeSpan LastInterval { get; set; }
    }
}
