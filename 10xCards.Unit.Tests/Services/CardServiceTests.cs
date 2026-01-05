using _10xCards.Application.DTOs.Cards;
using _10xCards.Application.Services;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace _10xCards.Unit.Tests.Services;

public class CardServiceTests
{
    private CardsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CardsDbContext(options);
    }

    private Mock<ILogger<CardService>> CreateMockLogger()
    {
        return new Mock<ILogger<CardService>>();
    }

    [Fact]
    public async Task CreateManualCardAsync_ValidInput_CreatesCard()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var front = "This is a valid front text with minimum fifty characters required for validation and testing";
        var back = "This is a valid back text with minimum fifty characters required for validation and testing";

        // Act
        var result = await service.CreateManualCardAsync(userId, front, back);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        var card = await context.Cards.FirstAsync(c => c.Id == result.Value);
        Assert.Equal(userId, card.UserId);
        Assert.Equal(front, card.Front);
        Assert.Equal(back, card.Back);
        Assert.Equal(GenerationMethod.Manual, card.GenerationMethod);

        var progress = await context.CardProgress.FirstAsync(p => p.CardId == card.Id);
        Assert.Equal(userId, progress.UserId);
        Assert.Equal(0, progress.ReviewCount);
        Assert.Equal("{}", progress.SrState);
    }

    [Fact]
    public async Task CreateManualCardAsync_FrontTooShort_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var front = "Too short";
        var back = "This is a valid back text with minimum fifty characters required for validation";

        // Act
        var result = await service.CreateManualCardAsync(userId, front, back);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Front must be between 50 and 500 characters", result.Error);
    }

    [Fact]
    public async Task CreateManualCardAsync_FrontTooLong_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var front = new string('a', 501);
        var back = "This is a valid back text with minimum fifty characters required for validation";

        // Act
        var result = await service.CreateManualCardAsync(userId, front, back);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Front must be between 50 and 500 characters", result.Error);
    }

    [Fact]
    public async Task CreateManualCardAsync_BackTooShort_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var front = "This is a valid front text with minimum fifty characters required for validation";
        var back = "Too short";

        // Act
        var result = await service.CreateManualCardAsync(userId, front, back);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Back must be between 50 and 500 characters", result.Error);
    }

    [Fact]
    public async Task CreateManualCardAsync_BackTooLong_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var front = "This is a valid front text with minimum fifty characters required for validation";
        var back = new string('a', 501);

        // Act
        var result = await service.CreateManualCardAsync(userId, front, back);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Back must be between 50 and 500 characters", result.Error);
    }

    [Fact]
    public async Task UpdateCardAsync_ValidInput_UpdatesCard()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Original front text with minimum fifty characters required for validation purposes",
            Back = "Original back text with minimum fifty characters required for validation purposes",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        await context.SaveChangesAsync();

        var newFront = "Updated front text with minimum fifty characters required for validation purposes";
        var newBack = "Updated back text with minimum fifty characters required for validation purposes";

        // Act
        var result = await service.UpdateCardAsync(cardId, userId, newFront, newBack);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedCard = await context.Cards.FirstAsync(c => c.Id == cardId);
        Assert.Equal(newFront, updatedCard.Front);
        Assert.Equal(newBack, updatedCard.Back);
    }

    [Fact]
    public async Task UpdateCardAsync_FrontTooShort_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Original front text with minimum fifty characters required for validation",
            Back = "Original back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateCardAsync(cardId, userId, "Short", "Valid back text with minimum fifty characters required for validation");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Front must be between 50 and 500 characters", result.Error);
    }

    [Fact]
    public async Task UpdateCardAsync_BackTooLong_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Original front text with minimum fifty characters required for validation",
            Back = "Original back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateCardAsync(
            cardId, 
            userId, 
            "Valid front text with minimum fifty characters required for validation",
            new string('a', 501));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Back must be between 50 and 500 characters", result.Error);
    }

    [Fact]
    public async Task UpdateCardAsync_WrongUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = ownerId,
            Front = "Original front text with minimum fifty characters required for validation",
            Back = "Original back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateCardAsync(
            cardId, 
            differentUserId, 
            "Updated front text with minimum fifty characters required for validation",
            "Updated back text with minimum fifty characters required for validation");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card not found or access denied", result.Error);
    }

    [Fact]
    public async Task GetCardsAsync_FilterAll_ReturnsAllUserCards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();

        await SeedCardsForUser(context, userId, 5);

        // Act
        var result = await service.GetCardsAsync(userId, CardFilter.All, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Cards.Count);
    }

    [Fact]
    public async Task GetCardsAsync_FilterDueToday_ReturnsDueCards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();

        // Create 3 due cards and 2 future cards
        await SeedCardsForUser(context, userId, 3, DateTime.UtcNow.AddDays(-1));
        await SeedCardsForUser(context, userId, 2, DateTime.UtcNow.AddDays(2));

        // Act
        var result = await service.GetCardsAsync(userId, CardFilter.DueToday, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalCount);
        Assert.Equal(3, result.Value.Cards.Count);
    }

    [Fact]
    public async Task GetCardsAsync_FilterNew_ReturnsNewCards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();

        // Create 2 new cards (ReviewCount = 0) and 3 reviewed cards
        await SeedCardsForUser(context, userId, 2, DateTime.UtcNow, 0);
        await SeedCardsForUser(context, userId, 3, DateTime.UtcNow, 5);

        // Act
        var result = await service.GetCardsAsync(userId, CardFilter.New, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(2, result.Value.Cards.Count);
        Assert.All(result.Value.Cards, card => Assert.Equal("New", card.StatusBadge));
    }

    [Fact]
    public async Task GetCardsAsync_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();

        await SeedCardsForUser(context, userId, 15);

        // Act
        var result = await service.GetCardsAsync(userId, CardFilter.All, 2, 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.TotalCount);
        Assert.Equal(5, result.Value.Cards.Count);
        Assert.Equal(2, result.Value.PageNumber);
        Assert.Equal(5, result.Value.PageSize);
    }

    [Fact]
    public async Task GetCardsAsync_StatusBadges_CalculatedCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();

        // Create new card
        await SeedCardsForUser(context, userId, 1, DateTime.UtcNow, 0);
        // Create due card
        await SeedCardsForUser(context, userId, 1, DateTime.UtcNow.AddDays(-1), 3);
        // Create learned card
        await SeedCardsForUser(context, userId, 1, DateTime.UtcNow.AddDays(5), 5);

        // Act
        var result = await service.GetCardsAsync(userId, CardFilter.All, 1, 10);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Cards.Count);
        
        var statusBadges = result.Value.Cards.Select(c => c.StatusBadge).ToList();
        Assert.Contains("New", statusBadges);
        Assert.Contains("Due", statusBadges);
        Assert.Contains("Learned", statusBadges);
    }

    [Fact]
    public async Task DeleteCardAsync_ValidCard_DeletesCard()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteCardAsync(cardId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(await context.Cards.AnyAsync(c => c.Id == cardId));
    }

    [Fact]
    public async Task DeleteCardAsync_WrongUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = ownerId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteCardAsync(cardId, differentUserId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card not found or access denied", result.Error);
        Assert.True(await context.Cards.AnyAsync(c => c.Id == cardId));
    }

    [Fact]
    public async Task GetDueCardsAsync_ReturnsDueCardsInOrder()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var logger = CreateMockLogger();
        var service = new CardService(context, logger.Object);
        var userId = Guid.NewGuid();

        // Create cards with different NextReviewUtc
        await SeedCardsForUser(context, userId, 2, DateTime.UtcNow.AddDays(-3));
        await SeedCardsForUser(context, userId, 3, DateTime.UtcNow.AddDays(-1));
        await SeedCardsForUser(context, userId, 2, DateTime.UtcNow.AddDays(2)); // Future cards

        // Act
        var result = await service.GetDueCardsAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Count);
    }

    private async Task SeedCardsForUser(
        CardsDbContext context, 
        Guid userId, 
        int count, 
        DateTime? nextReview = null,
        int reviewCount = 0)
    {
        nextReview ??= DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            var cardId = Guid.NewGuid();
            var card = new Card
            {
                Id = cardId,
                UserId = userId,
                Front = $"Test front text {i} with minimum fifty characters required for validation purposes",
                Back = $"Test back text {i} with minimum fifty characters required for validation purposes",
                GenerationMethod = GenerationMethod.Manual,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            var progress = new CardProgress
            {
                CardId = cardId,
                UserId = userId,
                ReviewCount = reviewCount,
                NextReviewUtc = nextReview.Value,
                SrState = "{}",
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            context.Cards.Add(card);
            context.CardProgress.Add(progress);
        }

        await context.SaveChangesAsync();
    }
}
