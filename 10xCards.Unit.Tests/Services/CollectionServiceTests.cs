using _10xCards.Application.Services;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace _10xCards.Unit.Tests.Services;

public class CollectionServiceTests
{
    private CardsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CardsDbContext(options);
    }

    private Mock<ILogger<CollectionService>> CreateMockLogger()
    {
        return new Mock<ILogger<CollectionService>>();
    }

    [Fact]
    public async Task CreateCollectionAsync_ValidInput_CreatesCollection()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardIds = await SeedCardsForUser(context, userId, 3);

        var name = "Test Collection";
        var description = "Test Description";

        // Act
        var result = await service.CreateCollectionAsync(userId, name, description, cardIds);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var collection = await context.CardCollections.FirstAsync(c => c.Id == result.Value);
        Assert.Equal(userId, collection.UserId);
        Assert.Equal(name, collection.Name);
        Assert.Equal(description, collection.Description);

        var collectionCards = await context.CardCollectionCards
            .Where(cc => cc.CollectionId == collection.Id)
            .ToListAsync();
        Assert.Equal(3, collectionCards.Count);
    }

    [Fact]
    public async Task CreateCollectionAsync_NameTooLong_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardIds = await SeedCardsForUser(context, userId, 1);

        var name = new string('a', 201);
        var description = "Test Description";

        // Act
        var result = await service.CreateCollectionAsync(userId, name, description, cardIds);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Collection name must be between 1 and 200 characters.", result.Error);
    }

    [Fact]
    public async Task CreateCollectionAsync_EmptyName_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardIds = await SeedCardsForUser(context, userId, 1);

        var name = "";
        var description = "Test Description";

        // Act
        var result = await service.CreateCollectionAsync(userId, name, description, cardIds);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Collection name must be between 1 and 200 characters.", result.Error);
    }

    [Fact]
    public async Task CreateCollectionAsync_DescriptionTooLong_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardIds = await SeedCardsForUser(context, userId, 1);

        var name = "Test Collection";
        var description = new string('a', 1001);

        // Act
        var result = await service.CreateCollectionAsync(userId, name, description, cardIds);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Collection description cannot exceed 1000 characters.", result.Error);
    }

    [Fact]
    public async Task CreateCollectionAsync_CardsNotBelongingToUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        
        var userCards = await SeedCardsForUser(context, userId, 2);
        var otherUserCards = await SeedCardsForUser(context, otherUserId, 1);

        var cardIds = userCards.Concat(otherUserCards).ToList();

        // Act
        var result = await service.CreateCollectionAsync(userId, "Test", "Description", cardIds);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Some cards do not exist or do not belong to you.", result.Error);
    }

    [Fact]
    public async Task UpdateCollectionAsync_ValidInput_UpdatesCollection()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Original Name",
            Description = "Original Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        var newName = "Updated Name";
        var newDescription = "Updated Description";

        // Act
        var result = await service.UpdateCollectionAsync(collectionId, userId, newName, newDescription);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedCollection = await context.CardCollections.FirstAsync(c => c.Id == collectionId);
        Assert.Equal(newName, updatedCollection.Name);
        Assert.Equal(newDescription, updatedCollection.Description);
    }

    [Fact]
    public async Task UpdateCollectionAsync_CreatesBackup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var originalName = "Original Name";
        var originalDescription = "Original Description";

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = originalName,
            Description = originalDescription,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateCollectionAsync(collectionId, userId, "New Name", "New Description");

        // Assert
        Assert.True(result.IsSuccess);

        var backup = await context.CardCollectionBackups.FirstAsync(b => b.CollectionId == collectionId);
        Assert.Equal(originalName, backup.PreviousName);
        Assert.Equal(originalDescription, backup.PreviousDescription);
    }

    [Fact]
    public async Task UpdateCollectionAsync_UpdatesExistingBackup()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Name Version 2",
            Description = "Description Version 2",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        var oldBackup = new CardCollectionBackup
        {
            CollectionId = collectionId,
            PreviousName = "Name Version 1",
            PreviousDescription = "Description Version 1",
            BackedUpUtc = DateTime.UtcNow.AddDays(-1)
        };

        context.CardCollections.Add(collection);
        context.CardCollectionBackups.Add(oldBackup);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateCollectionAsync(collectionId, userId, "Name Version 3", "Description Version 3");

        // Assert
        Assert.True(result.IsSuccess);

        var backups = await context.CardCollectionBackups.ToListAsync();
        Assert.Single(backups);
        
        var backup = backups.First();
        Assert.Equal("Name Version 2", backup.PreviousName);
        Assert.Equal("Description Version 2", backup.PreviousDescription);
    }

    [Fact]
    public async Task AddCardsToCollectionAsync_ValidCards_AddsCards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Test Collection",
            Description = "Test Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        var cardIds = await SeedCardsForUser(context, userId, 3);

        // Act
        var result = await service.AddCardsToCollectionAsync(collectionId, userId, cardIds);

        // Assert
        Assert.True(result.IsSuccess);

        var collectionCards = await context.CardCollectionCards
            .Where(cc => cc.CollectionId == collectionId)
            .ToListAsync();
        Assert.Equal(3, collectionCards.Count);
    }

    [Fact]
    public async Task AddCardsToCollectionAsync_DuplicateCards_SkipsDuplicates()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Test Collection",
            Description = "Test Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        
        var cardIds = await SeedCardsForUser(context, userId, 3);
        
        // Add first 2 cards to collection
        foreach (var cardId in cardIds.Take(2))
        {
            context.CardCollectionCards.Add(new CardCollectionCard
            {
                CollectionId = collectionId,
                CardId = cardId,
                AddedUtc = DateTime.UtcNow
            });
        }
        
        await context.SaveChangesAsync();

        // Act - try to add all 3 cards (2 duplicates + 1 new)
        var result = await service.AddCardsToCollectionAsync(collectionId, userId, cardIds);

        // Assert
        Assert.True(result.IsSuccess);

        var collectionCards = await context.CardCollectionCards
            .Where(cc => cc.CollectionId == collectionId)
            .ToListAsync();
        Assert.Equal(3, collectionCards.Count); // Should still be 3, not 5
    }

    [Fact]
    public async Task AddCardsToCollectionAsync_CardsFromOtherUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Test Collection",
            Description = "Test Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        var otherUserCards = await SeedCardsForUser(context, otherUserId, 2);

        // Act
        var result = await service.AddCardsToCollectionAsync(collectionId, userId, otherUserCards);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Some cards do not exist or do not belong to you.", result.Error);
    }

    [Fact]
    public async Task RemoveCardsFromCollectionAsync_ValidCards_RemovesCards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Test Collection",
            Description = "Test Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        
        var cardIds = await SeedCardsForUser(context, userId, 5);
        
        foreach (var cardId in cardIds)
        {
            context.CardCollectionCards.Add(new CardCollectionCard
            {
                CollectionId = collectionId,
                CardId = cardId,
                AddedUtc = DateTime.UtcNow
            });
        }
        
        await context.SaveChangesAsync();

        var cardsToRemove = cardIds.Take(2).ToList();

        // Act
        var result = await service.RemoveCardsFromCollectionAsync(collectionId, userId, cardsToRemove);

        // Assert
        Assert.True(result.IsSuccess);

        var remainingCards = await context.CardCollectionCards
            .Where(cc => cc.CollectionId == collectionId)
            .ToListAsync();
        Assert.Equal(3, remainingCards.Count);
    }

    [Fact]
    public async Task RestorePreviousVersionAsync_WithBackup_RestoresCollection()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var previousName = "Previous Name";
        var previousDescription = "Previous Description";

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Current Name",
            Description = "Current Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        var backup = new CardCollectionBackup
        {
            CollectionId = collectionId,
            PreviousName = previousName,
            PreviousDescription = previousDescription,
            BackedUpUtc = DateTime.UtcNow.AddHours(-1)
        };

        context.CardCollections.Add(collection);
        context.CardCollectionBackups.Add(backup);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RestorePreviousVersionAsync(collectionId, userId);

        // Assert
        Assert.True(result.IsSuccess);

        var restoredCollection = await context.CardCollections.FirstAsync(c => c.Id == collectionId);
        Assert.Equal(previousName, restoredCollection.Name);
        Assert.Equal(previousDescription, restoredCollection.Description);

        var backupExists = await context.CardCollectionBackups.AnyAsync(b => b.CollectionId == collectionId);
        Assert.False(backupExists);
    }

    [Fact]
    public async Task RestorePreviousVersionAsync_NoBackup_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Current Name",
            Description = "Current Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RestorePreviousVersionAsync(collectionId, userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No backup available for this collection.", result.Error);
    }

    [Fact]
    public async Task DeleteCollectionAsync_ValidCollection_DeletesCollection()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = userId,
            Name = "Test Collection",
            Description = "Test Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteCollectionAsync(collectionId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(await context.CardCollections.AnyAsync(c => c.Id == collectionId));
    }

    [Fact]
    public async Task DeleteCollectionAsync_WrongUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        var collection = new CardCollection
        {
            Id = collectionId,
            UserId = ownerId,
            Name = "Test Collection",
            Description = "Test Description",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.CardCollections.Add(collection);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteCollectionAsync(collectionId, differentUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Collection not found or access denied.", result.Error);
        Assert.True(await context.CardCollections.AnyAsync(c => c.Id == collectionId));
    }

    [Fact]
    public async Task GetCollectionsAsync_ReturnsPagedResults()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CollectionService(context, logger.Object);
        var userId = Guid.NewGuid();

        for (int i = 0; i < 15; i++)
        {
            context.CardCollections.Add(new CardCollection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = $"Collection {i}",
                Description = $"Description {i}",
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetCollectionsAsync(userId, 2, 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Collections.Count);
        Assert.Equal(2, result.Value.Page);
    }

    private async Task<List<Guid>> SeedCardsForUser(CardsDbContext context, Guid userId, int count)
    {
        var cardIds = new List<Guid>();

        for (int i = 0; i < count; i++)
        {
            var cardId = Guid.NewGuid();
            var card = new Card
            {
                Id = cardId,
                UserId = userId,
                Front = $"Test front {i} with minimum fifty characters required for validation purposes",
                Back = $"Test back {i} with minimum fifty characters required for validation purposes",
                GenerationMethod = GenerationMethod.Manual,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            context.Cards.Add(card);
            cardIds.Add(cardId);
        }

        await context.SaveChangesAsync();
        return cardIds;
    }
}
