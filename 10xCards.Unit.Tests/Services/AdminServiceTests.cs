using _10xCards.Application.Services;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace _10xCards.Unit.Tests.Services;

public class AdminServiceTests
{
    private CardsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CardsDbContext(options);
    }

    private IMemoryCache CreateMemoryCache()
    {
        return new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_NoData_ReturnsZeroMetrics()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(0, result.Value.TotalCards);
        Assert.Equal(0, result.Value.TotalAiCards);
        Assert.Equal(0, result.Value.TotalManualCards);
        Assert.Equal(0, result.Value.AiAcceptanceRate);
        Assert.Equal(0, result.Value.ActiveUsers7d);
        Assert.Equal(0, result.Value.ActiveUsers30d);
        Assert.Equal(0, result.Value.GenerationErrorsLast7d);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_WithCards_CalculatesCorrectCounts()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);
        var userId = Guid.NewGuid();

        // Create 5 AI cards and 3 manual cards
        await SeedCards(context, userId, 5, GenerationMethod.Ai);
        await SeedCards(context, userId, 3, GenerationMethod.Manual);

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Value!.TotalCards);
        Assert.Equal(5, result.Value.TotalAiCards);
        Assert.Equal(3, result.Value.TotalManualCards);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_WithAcceptanceEvents_CalculatesAcceptanceRate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);
        var userId = Guid.NewGuid();

        // User needs at least 5 cards to be eligible
        await SeedCards(context, userId, 5, GenerationMethod.Ai);

        // Create acceptance events: 7 accepted, 3 rejected
        for (int i = 0; i < 7; i++)
        {
            context.AcceptanceEvents.Add(new AcceptanceEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProposalId = Guid.NewGuid(),
                Action = AcceptanceAction.Accepted,
                CreatedUtc = DateTime.UtcNow
            });
        }

        for (int i = 0; i < 3; i++)
        {
            context.AcceptanceEvents.Add(new AcceptanceEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProposalId = Guid.NewGuid(),
                Action = AcceptanceAction.Rejected,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        // 7 accepted out of 10 total = 0.7
        Assert.Equal(0.7, result.Value!.AiAcceptanceRate);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_OnlyCountsUsersWithMinimum5Cards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);
        
        var user1 = Guid.NewGuid(); // 5 cards - eligible
        var user2 = Guid.NewGuid(); // 3 cards - not eligible

        await SeedCards(context, user1, 5, GenerationMethod.Ai);
        await SeedCards(context, user2, 3, GenerationMethod.Ai);

        // Create acceptance events for both users
        context.AcceptanceEvents.Add(new AcceptanceEvent
        {
            Id = Guid.NewGuid(),
            UserId = user1,
            ProposalId = Guid.NewGuid(),
            Action = AcceptanceAction.Accepted,
            CreatedUtc = DateTime.UtcNow
        });

        context.AcceptanceEvents.Add(new AcceptanceEvent
        {
            Id = Guid.NewGuid(),
            UserId = user2,
            ProposalId = Guid.NewGuid(),
            Action = AcceptanceAction.Rejected,
            CreatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        // Only user1's events should count, so 1 accepted out of 1 = 1.0
        Assert.Equal(1.0, result.Value!.AiAcceptanceRate);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_CalculatesPercentiles()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);

        // Create 10 users with different acceptance rates
        var users = new List<Guid>();
        for (int i = 0; i < 10; i++)
        {
            var userId = Guid.NewGuid();
            users.Add(userId);
            await SeedCards(context, userId, 5, GenerationMethod.Ai);

            // Create varying acceptance rates
            var acceptedCount = i; // 0-9 accepted
            var rejectedCount = 10 - i; // 10-1 rejected

            for (int j = 0; j < acceptedCount; j++)
            {
                context.AcceptanceEvents.Add(new AcceptanceEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProposalId = Guid.NewGuid(),
                    Action = AcceptanceAction.Accepted,
                    CreatedUtc = DateTime.UtcNow
                });
            }

            for (int j = 0; j < rejectedCount; j++)
            {
                context.AcceptanceEvents.Add(new AcceptanceEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProposalId = Guid.NewGuid(),
                    Action = AcceptanceAction.Rejected,
                    CreatedUtc = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.AcceptancePercentiles);
        Assert.True(result.Value.AcceptancePercentiles.P50 >= 0 && result.Value.AcceptancePercentiles.P50 <= 1);
        Assert.True(result.Value.AcceptancePercentiles.P75 >= 0 && result.Value.AcceptancePercentiles.P75 <= 1);
        Assert.True(result.Value.AcceptancePercentiles.P90 >= 0 && result.Value.AcceptancePercentiles.P90 <= 1);
        
        // P75 should be >= P50, P90 should be >= P75
        Assert.True(result.Value.AcceptancePercentiles.P75 >= result.Value.AcceptancePercentiles.P50);
        Assert.True(result.Value.AcceptancePercentiles.P90 >= result.Value.AcceptancePercentiles.P75);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_NoEligibleUsers_ReturnsZeroAcceptanceRate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);
        var userId = Guid.NewGuid();

        // User has only 3 cards, not eligible
        await SeedCards(context, userId, 3, GenerationMethod.Ai);

        context.AcceptanceEvents.Add(new AcceptanceEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProposalId = Guid.NewGuid(),
            Action = AcceptanceAction.Accepted,
            CreatedUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.AiAcceptanceRate);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_CalculatesActiveUsersLast7Days()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        // User1: active 2 days ago
        await SeedCards(context, user1, 1, GenerationMethod.Manual, DateTime.UtcNow.AddDays(-2));
        
        // User2: active 10 days ago (not in last 7 days)
        await SeedCards(context, user2, 1, GenerationMethod.Manual, DateTime.UtcNow.AddDays(-10));
        
        // User3: active today
        await SeedCards(context, user3, 1, GenerationMethod.Manual, DateTime.UtcNow);

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.ActiveUsers7d);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_CalculatesActiveUsersLast30Days()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var user3 = Guid.NewGuid();

        // User1: active 20 days ago
        await SeedCards(context, user1, 1, GenerationMethod.Manual, DateTime.UtcNow.AddDays(-20));
        
        // User2: active 40 days ago (not in last 30 days)
        await SeedCards(context, user2, 1, GenerationMethod.Manual, DateTime.UtcNow.AddDays(-40));
        
        // User3: active today
        await SeedCards(context, user3, 1, GenerationMethod.Manual, DateTime.UtcNow);

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.ActiveUsers30d);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_CountsGenerationErrorsLast7Days()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);

        // 3 errors in last 7 days
        for (int i = 0; i < 3; i++)
        {
            context.GenerationErrors.Add(new GenerationError
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ErrorMessage = $"Error {i}",
                CreatedUtc = DateTime.UtcNow.AddDays(-i)
            });
        }

        // 2 errors older than 7 days
        for (int i = 0; i < 2; i++)
        {
            context.GenerationErrors.Add(new GenerationError
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ErrorMessage = $"Old Error {i}",
                CreatedUtc = DateTime.UtcNow.AddDays(-10 - i)
            });
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.GenerationErrorsLast7d);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_UsesCacheOnSecondCall()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);

        await SeedCards(context, Guid.NewGuid(), 5, GenerationMethod.Ai);

        // Act
        var result1 = await service.GetDashboardMetricsAsync();
        
        // Add more cards after first call
        await SeedCards(context, Guid.NewGuid(), 3, GenerationMethod.Manual);
        
        var result2 = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        
        // Both should return same result due to caching
        Assert.Equal(result1.Value!.TotalCards, result2.Value!.TotalCards);
        Assert.Equal(5, result1.Value.TotalCards);
        Assert.Equal(5, result2.Value.TotalCards); // Should be cached, not 8
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_PercentilesWithSingleUser_ReturnsUserRate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);
        var userId = Guid.NewGuid();

        await SeedCards(context, userId, 5, GenerationMethod.Ai);

        // 8 accepted, 2 rejected = 0.8 acceptance rate
        for (int i = 0; i < 8; i++)
        {
            context.AcceptanceEvents.Add(new AcceptanceEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProposalId = Guid.NewGuid(),
                Action = AcceptanceAction.Accepted,
                CreatedUtc = DateTime.UtcNow
            });
        }

        for (int i = 0; i < 2; i++)
        {
            context.AcceptanceEvents.Add(new AcceptanceEvent
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ProposalId = Guid.NewGuid(),
                Action = AcceptanceAction.Rejected,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.AcceptancePercentiles);
        // With single user, all percentiles should be the same (0.8)
        Assert.Equal(0.8, result.Value.AcceptancePercentiles.P50);
        Assert.Equal(0.8, result.Value.AcceptancePercentiles.P75);
        Assert.Equal(0.8, result.Value.AcceptancePercentiles.P90);
    }

    [Fact]
    public async Task GetDashboardMetricsAsync_NoAcceptanceEvents_ReturnsZeroAcceptanceRate()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var cache = CreateMemoryCache();
        var service = new AdminService(context, cache);
        var userId = Guid.NewGuid();

        await SeedCards(context, userId, 5, GenerationMethod.Ai);

        // Act
        var result = await service.GetDashboardMetricsAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.AiAcceptanceRate);
    }

    private async Task SeedCards(
        CardsDbContext context,
        Guid userId,
        int count,
        GenerationMethod method,
        DateTime? createdUtc = null)
    {
        createdUtc ??= DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            var card = new Card
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Front = $"Test front {i} with minimum fifty characters required for validation purposes",
                Back = $"Test back {i} with minimum fifty characters required for validation purposes",
                GenerationMethod = method,
                CreatedUtc = createdUtc.Value,
                UpdatedUtc = createdUtc.Value
            };

            context.Cards.Add(card);
        }

        await context.SaveChangesAsync();
    }
}
