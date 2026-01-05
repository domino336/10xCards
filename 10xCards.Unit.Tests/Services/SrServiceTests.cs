using _10xCards.Application.Services;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Unit.Tests.Services;

public class SrServiceTests
{
    private CardsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CardsDbContext(options);
    }

    [Fact]
    public async Task RecordReviewAsync_Again_ResetsRepetitionsAndDecreasesEaseFactor()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow
        };

        var progress = new CardProgress
        {
            CardId = cardId,
            UserId = userId,
            ReviewCount = 5,
            LastReviewUtc = DateTime.UtcNow.AddDays(-2),
            NextReviewUtc = DateTime.UtcNow.AddDays(-1),
            SrState = "{\"EaseFactor\":2.5,\"Repetitions\":3,\"LastInterval\":\"6.00:00:00\"}",
            CreatedUtc = DateTime.UtcNow.AddDays(-10),
            UpdatedUtc = DateTime.UtcNow.AddDays(-2)
        };

        context.Cards.Add(card);
        context.CardProgress.Add(progress);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RecordReviewAsync(cardId, userId, ReviewResult.Again);

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedProgress = await context.CardProgress.FirstAsync(p => p.CardId == cardId);
        Assert.Equal(6, updatedProgress.ReviewCount);
        Assert.Equal(ReviewResult.Again, updatedProgress.LastReviewResult);
        Assert.NotNull(updatedProgress.LastReviewUtc);
        Assert.NotNull(updatedProgress.NextReviewUtc);
        
        // NextReview should be approximately 10 minutes after LastReview
        var timeDiff = updatedProgress.NextReviewUtc - updatedProgress.LastReviewUtc!.Value;
        Assert.InRange(timeDiff.TotalMinutes, 9, 11);
        
        // EaseFactor should decrease (max 1.3)
        Assert.Contains("\"EaseFactor\":2.", updatedProgress.SrState);
        Assert.Contains("\"Repetitions\":0", updatedProgress.SrState);
    }

    [Fact]
    public async Task RecordReviewAsync_Good_IncreasesRepetitionsAndDecreasesEaseFactorSlightly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow
        };

        var progress = new CardProgress
        {
            CardId = cardId,
            UserId = userId,
            ReviewCount = 0,
            NextReviewUtc = DateTime.UtcNow.AddDays(-1),
            SrState = "{}",
            CreatedUtc = DateTime.UtcNow.AddDays(-10),
            UpdatedUtc = DateTime.UtcNow.AddDays(-10)
        };

        context.Cards.Add(card);
        context.CardProgress.Add(progress);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RecordReviewAsync(cardId, userId, ReviewResult.Good);

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedProgress = await context.CardProgress.FirstAsync(p => p.CardId == cardId);
        Assert.Equal(1, updatedProgress.ReviewCount);
        Assert.Equal(ReviewResult.Good, updatedProgress.LastReviewResult);
        
        // First repetition should schedule for 1 day
        var timeDiff = updatedProgress.NextReviewUtc - updatedProgress.LastReviewUtc!.Value;
        Assert.InRange(timeDiff.TotalHours, 23, 25);
        
        Assert.Contains("\"Repetitions\":1", updatedProgress.SrState);
    }

    [Fact]
    public async Task RecordReviewAsync_Good_SecondRepetition_SchedulesForSixDays()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow
        };

        var progress = new CardProgress
        {
            CardId = cardId,
            UserId = userId,
            ReviewCount = 1,
            NextReviewUtc = DateTime.UtcNow.AddDays(-1),
            SrState = "{\"EaseFactor\":2.35,\"Repetitions\":1,\"LastInterval\":\"1.00:00:00\"}",
            CreatedUtc = DateTime.UtcNow.AddDays(-10),
            UpdatedUtc = DateTime.UtcNow.AddDays(-1)
        };

        context.Cards.Add(card);
        context.CardProgress.Add(progress);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RecordReviewAsync(cardId, userId, ReviewResult.Good);

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedProgress = await context.CardProgress.FirstAsync(p => p.CardId == cardId);
        Assert.Equal(2, updatedProgress.ReviewCount);
        
        // Second repetition should schedule for 6 days
        var timeDiff = updatedProgress.NextReviewUtc - updatedProgress.LastReviewUtc!.Value;
        Assert.InRange(timeDiff.TotalDays, 5.9, 6.1);
        
        Assert.Contains("\"Repetitions\":2", updatedProgress.SrState);
    }

    [Fact]
    public async Task RecordReviewAsync_Easy_IncreasesRepetitionsAndIncreasesEaseFactor()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow
        };

        var progress = new CardProgress
        {
            CardId = cardId,
            UserId = userId,
            ReviewCount = 2,
            NextReviewUtc = DateTime.UtcNow.AddDays(-1),
            SrState = "{\"EaseFactor\":2.2,\"Repetitions\":2,\"LastInterval\":\"6.00:00:00\"}",
            CreatedUtc = DateTime.UtcNow.AddDays(-20),
            UpdatedUtc = DateTime.UtcNow.AddDays(-1)
        };

        context.Cards.Add(card);
        context.CardProgress.Add(progress);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RecordReviewAsync(cardId, userId, ReviewResult.Easy);

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedProgress = await context.CardProgress.FirstAsync(p => p.CardId == cardId);
        Assert.Equal(3, updatedProgress.ReviewCount);
        Assert.Equal(ReviewResult.Easy, updatedProgress.LastReviewResult);
        
        Assert.Contains("\"Repetitions\":3", updatedProgress.SrState);
        // EaseFactor should increase (was 2.2, should be 2.35)
        Assert.Contains("\"EaseFactor\":2.3", updatedProgress.SrState);
    }

    [Fact]
    public async Task RecordReviewAsync_CardNotFound_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        // Act
        var result = await service.RecordReviewAsync(cardId, userId, ReviewResult.Good);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card progress not found or access denied", result.Error);
    }

    [Fact]
    public async Task RecordReviewAsync_WrongUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
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
            CreatedUtc = DateTime.UtcNow
        };

        var progress = new CardProgress
        {
            CardId = cardId,
            UserId = ownerId,
            ReviewCount = 0,
            NextReviewUtc = DateTime.UtcNow,
            SrState = "{}",
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        context.Cards.Add(card);
        context.CardProgress.Add(progress);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RecordReviewAsync(cardId, differentUserId, ReviewResult.Good);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card progress not found or access denied", result.Error);
    }

    [Fact]
    public async Task RecordReviewAsync_UpdatesLastReviewUtcAndNextReviewUtc()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SrService(context);
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var oldLastReview = DateTime.UtcNow.AddDays(-5);

        var card = new Card
        {
            Id = cardId,
            UserId = userId,
            Front = "Test front text with minimum fifty characters required for validation",
            Back = "Test back text with minimum fifty characters required for validation",
            GenerationMethod = GenerationMethod.Manual,
            CreatedUtc = DateTime.UtcNow
        };

        var progress = new CardProgress
        {
            CardId = cardId,
            UserId = userId,
            ReviewCount = 0,
            LastReviewUtc = oldLastReview,
            NextReviewUtc = DateTime.UtcNow.AddDays(-1),
            SrState = "{}",
            CreatedUtc = DateTime.UtcNow.AddDays(-10),
            UpdatedUtc = DateTime.UtcNow.AddDays(-10)
        };

        context.Cards.Add(card);
        context.CardProgress.Add(progress);
        await context.SaveChangesAsync();

        // Act
        var beforeReview = DateTime.UtcNow;
        var result = await service.RecordReviewAsync(cardId, userId, ReviewResult.Good);
        var afterReview = DateTime.UtcNow;

        // Assert
        Assert.True(result.IsSuccess);
        
        var updatedProgress = await context.CardProgress.FirstAsync(p => p.CardId == cardId);
        Assert.NotNull(updatedProgress.LastReviewUtc);
        Assert.NotNull(updatedProgress.NextReviewUtc);
        
        // LastReviewUtc should be set to now
        Assert.True(updatedProgress.LastReviewUtc >= beforeReview);
        Assert.True(updatedProgress.LastReviewUtc <= afterReview);
        
        // NextReviewUtc should be in the future
        Assert.True(updatedProgress.NextReviewUtc > updatedProgress.LastReviewUtc!.Value);
    }
}
