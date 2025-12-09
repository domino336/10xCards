using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Cards;
using _10xCards.Persistance;
using _10xCards.Domain.Entities;

namespace _10xCards.Application.Services;

public sealed class CardService : ICardService
{
    private readonly CardsDbContext _db;
    private readonly ILogger<CardService> _logger;
    
    public CardService(CardsDbContext db, ILogger<CardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<int> GetTotalCardsAsync(CancellationToken cancellationToken = default)
        => _db.Cards.CountAsync(cancellationToken);

    public async Task<Result<Guid>> CreateManualCardAsync(
        Guid userId,
        string front,
        string back,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (front.Length < 50 || front.Length > 500)
                return Result<Guid>.Failure("Front must be between 50 and 500 characters");
            
            if (back.Length < 50 || back.Length > 500)
                return Result<Guid>.Failure("Back must be between 50 and 500 characters");

            var now = DateTime.UtcNow;
            var card = new Card
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Front = front,
                Back = back,
                GenerationMethod = GenerationMethod.Manual,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            var progress = new CardProgress
            {
                CardId = card.Id,
                UserId = userId,
                NextReviewUtc = now,
                ReviewCount = 0,
                SrState = "{}",
                CreatedUtc = now,
                UpdatedUtc = now
            };

            _db.Cards.Add(card);
            _db.CardProgress.Add(progress);
            
            await _db.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Manual card created: {CardId} by user {UserId}", card.Id, userId);
            
            return Result<Guid>.Success(card.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create manual card for user {UserId}", userId);
            return Result<Guid>.Failure($"Failed to create card: {ex.Message}");
        }
    }

    public async Task<Result<PagedCardsResponse>> GetCardsAsync(
        Guid userId,
        CardFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return Result<PagedCardsResponse>.Failure("Page number must be greater than 0");
        
        if (pageSize < 1 || pageSize > 100)
            return Result<PagedCardsResponse>.Failure("Page size must be between 1 and 100");

        IQueryable<Card> query = _db.Cards
            .Where(c => c.UserId == userId)
            .Include(c => c.Progress);

        query = filter switch
        {
            CardFilter.DueToday => query.Where(c => c.Progress!.NextReviewUtc <= DateTime.UtcNow),
            CardFilter.New => query.Where(c => c.Progress!.ReviewCount == 0),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var cards = await query
            .OrderBy(c => c.Progress!.NextReviewUtc)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(c => new CardListItemDto(
                c.Id,
                c.Front.Length > 100 ? c.Front.Substring(0, 97) + "..." : c.Front,
                c.Progress!.NextReviewUtc,
                c.Progress.ReviewCount,
                GetStatusBadge(c.Progress.NextReviewUtc, c.Progress.ReviewCount)
            ))
            .ToListAsync(cancellationToken);

        var response = new PagedCardsResponse(
            cards,
            totalCount,
            page,
            pageSize
        );

        return Result<PagedCardsResponse>.Success(response);
    }

    public async Task<Result> UpdateCardAsync(
        Guid cardId,
        Guid userId,
        string front,
        string back,
        CancellationToken cancellationToken = default)
    {
        if (front.Length < 50 || front.Length > 500)
            return Result.Failure("Front must be between 50 and 500 characters");
        
        if (back.Length < 50 || back.Length > 500)
            return Result.Failure("Back must be between 50 and 500 characters");

        var card = await _db.Cards
            .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, cancellationToken);

        if (card is null)
            return Result.Failure("Card not found or access denied");

        card.Front = front;
        card.Back = back;
        card.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }

    public async Task<Result> DeleteCardAsync(
        Guid cardId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var card = await _db.Cards
                .FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == userId, cancellationToken);

            if (card is null)
            {
                _logger.LogWarning("Delete card failed: Card {CardId} not found for user {UserId}", cardId, userId);
                return Result.Failure("Card not found or access denied");
            }

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Card deleted: {CardId} by user {UserId}", cardId, userId);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete card {CardId} for user {UserId}", cardId, userId);
            return Result.Failure($"Failed to delete card: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<ReviewCardDto>>> GetDueCardsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            const int maxCardsPerSession = 50;

            var dueCards = await _db.Cards
                .Where(c => c.UserId == userId)
                .Include(c => c.Progress)
                .Where(c => c.Progress!.NextReviewUtc <= now)
                .OrderBy(c => c.Progress!.NextReviewUtc)
                .ThenBy(c => c.Id)
                .Take(maxCardsPerSession)
                .AsNoTracking()
                .Select(c => new ReviewCardDto(
                    c.Id,
                    c.Front,
                    c.Back
                ))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<ReviewCardDto>>.Success(dueCards);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ReviewCardDto>>.Failure($"Failed to load due cards: {ex.Message}");
        }
    }

    private static string GetStatusBadge(DateTime nextReviewUtc, int reviewCount)
    {
        if (reviewCount == 0)
            return "New";
        
        if (nextReviewUtc <= DateTime.UtcNow)
            return "Due";
        
        return "Learned";
    }
}
