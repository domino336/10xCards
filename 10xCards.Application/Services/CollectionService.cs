using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Collections;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;

namespace _10xCards.Application.Services;

public sealed class CollectionService : ICollectionService
{
    private readonly CardsDbContext _context;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(CardsDbContext context, ILogger<CollectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Guid>> CreateCollectionAsync(
        Guid userId,
        string name,
        string description,
        List<Guid> cardIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
                return Result<Guid>.Failure("Collection name must be between 1 and 200 characters.");

            if (description?.Length > 1000)
                return Result<Guid>.Failure("Collection description cannot exceed 1000 characters.");

            // Verify all cards belong to user
            var userCardIds = await _context.Cards
                .Where(c => c.UserId == userId && cardIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (userCardIds.Count != cardIds.Count)
                return Result<Guid>.Failure("Some cards do not exist or do not belong to you.");

            var collection = new CardCollection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name.Trim(),
                Description = description?.Trim() ?? string.Empty,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            _context.CardCollections.Add(collection);

            foreach (var cardId in cardIds)
            {
                _context.CardCollectionCards.Add(new CardCollectionCard
                {
                    CollectionId = collection.Id,
                    CardId = cardId,
                    AddedUtc = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection {CollectionId} created by user {UserId} with {CardCount} cards",
                collection.Id, userId, cardIds.Count);

            return Result<Guid>.Success(collection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating collection for user {UserId}", userId);
            return Result<Guid>.Failure("Failed to create collection. Please try again.");
        }
    }

    public async Task<Result<CollectionDetailDto>> GetCollectionAsync(
        Guid collectionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _context.CardCollections
                .Where(c => c.Id == collectionId && c.UserId == userId)
                .Select(c => new CollectionDetailDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CreatedUtc,
                    c.UpdatedUtc,
                    _context.CardCollectionBackups.Any(b => b.CollectionId == c.Id),
                    c.CardCollectionCards.Select(cc => new CollectionCardDto(
                        cc.CardId,
                        cc.Card.Front,
                        cc.Card.Back,
                        cc.AddedUtc
                    )).ToList()
                ))
                .FirstOrDefaultAsync(cancellationToken);

            if (collection == null)
                return Result<CollectionDetailDto>.Failure("Collection not found or access denied.");

            return Result<CollectionDetailDto>.Success(collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collection {CollectionId} for user {UserId}", collectionId, userId);
            return Result<CollectionDetailDto>.Failure("Failed to retrieve collection. Please try again.");
        }
    }

    public async Task<Result<PagedCollectionsResponse>> GetCollectionsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var query = _context.CardCollections
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UpdatedUtc);

            var totalCount = await query.CountAsync(cancellationToken);

            var collections = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CollectionDto(
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CardCollectionCards.Count,
                    c.CreatedUtc,
                    c.UpdatedUtc,
                    _context.CardCollectionBackups.Any(b => b.CollectionId == c.Id)
                ))
                .ToListAsync(cancellationToken);

            var response = new PagedCollectionsResponse(collections, totalCount, page, pageSize);
            return Result<PagedCollectionsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving collections for user {UserId}", userId);
            return Result<PagedCollectionsResponse>.Failure("Failed to retrieve collections. Please try again.");
        }
    }

    public async Task<Result> UpdateCollectionAsync(
        Guid collectionId,
        Guid userId,
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
                return Result.Failure("Collection name must be between 1 and 200 characters.");

            if (description?.Length > 1000)
                return Result.Failure("Collection description cannot exceed 1000 characters.");

            var collection = await _context.CardCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId, cancellationToken);

            if (collection == null)
                return Result.Failure("Collection not found or access denied.");

            // Create backup before updating
            var existingBackup = await _context.CardCollectionBackups
                .FirstOrDefaultAsync(b => b.CollectionId == collectionId, cancellationToken);

            if (existingBackup != null)
            {
                existingBackup.PreviousName = collection.Name;
                existingBackup.PreviousDescription = collection.Description;
                existingBackup.BackedUpUtc = DateTime.UtcNow;
            }
            else
            {
                _context.CardCollectionBackups.Add(new CardCollectionBackup
                {
                    CollectionId = collectionId,
                    PreviousName = collection.Name,
                    PreviousDescription = collection.Description,
                    BackedUpUtc = DateTime.UtcNow
                });
            }

            collection.Name = name.Trim();
            collection.Description = description?.Trim() ?? string.Empty;
            collection.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection {CollectionId} updated by user {UserId}", collectionId, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating collection {CollectionId} for user {UserId}", collectionId, userId);
            return Result.Failure("Failed to update collection. Please try again.");
        }
    }

    public async Task<Result> AddCardsToCollectionAsync(
        Guid collectionId,
        Guid userId,
        List<Guid> cardIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _context.CardCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId, cancellationToken);

            if (collection == null)
                return Result.Failure("Collection not found or access denied.");

            // Verify all cards belong to user
            var userCardIds = await _context.Cards
                .Where(c => c.UserId == userId && cardIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            if (userCardIds.Count != cardIds.Count)
                return Result.Failure("Some cards do not exist or do not belong to you.");

            // Get existing card IDs in collection
            var existingCardIds = await _context.CardCollectionCards
                .Where(cc => cc.CollectionId == collectionId)
                .Select(cc => cc.CardId)
                .ToListAsync(cancellationToken);

            var newCardIds = cardIds.Except(existingCardIds).ToList();

            foreach (var cardId in newCardIds)
            {
                _context.CardCollectionCards.Add(new CardCollectionCard
                {
                    CollectionId = collectionId,
                    CardId = cardId,
                    AddedUtc = DateTime.UtcNow
                });
            }

            collection.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added {Count} cards to collection {CollectionId} by user {UserId}",
                newCardIds.Count, collectionId, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding cards to collection {CollectionId} for user {UserId}", collectionId, userId);
            return Result.Failure("Failed to add cards to collection. Please try again.");
        }
    }

    public async Task<Result> RemoveCardsFromCollectionAsync(
        Guid collectionId,
        Guid userId,
        List<Guid> cardIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _context.CardCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId, cancellationToken);

            if (collection == null)
                return Result.Failure("Collection not found or access denied.");

            var cardsToRemove = await _context.CardCollectionCards
                .Where(cc => cc.CollectionId == collectionId && cardIds.Contains(cc.CardId))
                .ToListAsync(cancellationToken);

            _context.CardCollectionCards.RemoveRange(cardsToRemove);

            collection.UpdatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed {Count} cards from collection {CollectionId} by user {UserId}",
                cardsToRemove.Count, collectionId, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cards from collection {CollectionId} for user {UserId}", collectionId, userId);
            return Result.Failure("Failed to remove cards from collection. Please try again.");
        }
    }

    public async Task<Result> DeleteCollectionAsync(
        Guid collectionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _context.CardCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId, cancellationToken);

            if (collection == null)
                return Result.Failure("Collection not found or access denied.");

            _context.CardCollections.Remove(collection);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection {CollectionId} deleted by user {UserId}", collectionId, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting collection {CollectionId} for user {UserId}", collectionId, userId);
            return Result.Failure("Failed to delete collection. Please try again.");
        }
    }

    public async Task<Result> RestorePreviousVersionAsync(
        Guid collectionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var collection = await _context.CardCollections
                .FirstOrDefaultAsync(c => c.Id == collectionId && c.UserId == userId, cancellationToken);

            if (collection == null)
                return Result.Failure("Collection not found or access denied.");

            var backup = await _context.CardCollectionBackups
                .FirstOrDefaultAsync(b => b.CollectionId == collectionId, cancellationToken);

            if (backup == null)
                return Result.Failure("No backup available for this collection.");

            collection.Name = backup.PreviousName;
            collection.Description = backup.PreviousDescription;
            collection.UpdatedUtc = DateTime.UtcNow;

            _context.CardCollectionBackups.Remove(backup);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Collection {CollectionId} restored to previous version by user {UserId}",
                collectionId, userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring collection {CollectionId} for user {UserId}", collectionId, userId);
            return Result.Failure("Failed to restore collection. Please try again.");
        }
    }
}
