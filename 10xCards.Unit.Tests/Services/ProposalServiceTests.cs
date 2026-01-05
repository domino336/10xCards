using _10xCards.Application.DTOs.Proposals;
using _10xCards.Application.Services;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Unit.Tests.Services;

public class ProposalServiceTests
{
    private CardsDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CardsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new CardsDbContext(options);
    }

    [Fact]
    public async Task GenerateProposalsAsync_ValidText_GeneratesProposals()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = "This is the first sentence with enough content. " +
                        "This is the second sentence with more information. " +
                        "This is the third sentence that adds details. " +
                        "This is the fourth sentence continuing the topic. " +
                        "This is the fifth sentence providing more context.";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Proposals);
        
        var sourceTextEntity = await context.SourceTexts.FirstAsync();
        Assert.Equal(userId, sourceTextEntity.UserId);
        Assert.Equal(sourceText, sourceTextEntity.Content);
        
        var proposals = await context.CardProposals.ToListAsync();
        Assert.NotEmpty(proposals);
        Assert.All(proposals, p =>
        {
            Assert.Equal(userId, p.UserId);
            Assert.True(p.Front.Length >= 50);
            Assert.True(p.Front.Length <= 500);
            Assert.True(p.Back.Length >= 50);
            Assert.True(p.Back.Length <= 500);
        });
    }

    [Fact]
    public async Task GenerateProposalsAsync_TextTooShort_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = "This text is too short";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Source text must be between 50 and 20,000 characters", result.Error);
    }

    [Fact]
    public async Task GenerateProposalsAsync_TextTooLong_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = new string('a', 20001);

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Source text must be between 50 and 20,000 characters", result.Error);
    }

    [Fact]
    public async Task GenerateProposalsAsync_EmptyText_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = "";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Source text must be between 50 and 20,000 characters", result.Error);
    }

    [Fact]
    public async Task GenerateProposalsAsync_WhitespaceOnly_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = "     ";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Source text must be between 50 and 20,000 characters", result.Error);
    }

    [Fact]
    public async Task GenerateProposalsAsync_LongTextWithManySentences_GeneratesMultipleProposals()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sentences = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            sentences.Add($"This is sentence number {i} with enough content to be valid and useful for testing purposes.");
        }
        var sourceText = string.Join(" ", sentences);

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        // Should generate at most 5 proposals
        Assert.True(result.Value.Proposals.Count <= 5);
        Assert.True(result.Value.Proposals.Count > 0);
    }

    [Fact]
    public async Task GenerateProposalsAsync_ShortTextWithFewSentences_GeneratesAtLeastOneProposal()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = "This is a single sentence with enough content for the minimum requirement of fifty characters.";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value.Proposals);
        Assert.True(result.Value.Proposals.Count >= 1);
    }

    [Fact]
    public async Task GetProposalsBySourceAsync_ReturnsProposalsForSource()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceTextId = Guid.NewGuid();

        var sourceText = new SourceText
        {
            Id = sourceTextId,
            UserId = userId,
            Content = "Test content with minimum fifty characters for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        var proposals = new List<CardProposal>
        {
            new CardProposal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceTextId = sourceTextId,
                Front = "Test front 1 with minimum fifty characters required for validation purposes",
                Back = "Test back 1 with minimum fifty characters required for validation purposes",
                CreatedUtc = DateTime.UtcNow
            },
            new CardProposal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceTextId = sourceTextId,
                Front = "Test front 2 with minimum fifty characters required for validation purposes",
                Back = "Test back 2 with minimum fifty characters required for validation purposes",
                CreatedUtc = DateTime.UtcNow
            }
        };

        context.SourceTexts.Add(sourceText);
        context.CardProposals.AddRange(proposals);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProposalsBySourceAsync(userId, sourceTextId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, p => Assert.Equal(sourceTextId, p.SourceTextId));
    }

    [Fact]
    public async Task AcceptProposalsAsync_ValidProposals_CreatesCards()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceTextId = Guid.NewGuid();

        var proposal1 = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceTextId = sourceTextId,
            Front = "Proposal front 1 with minimum fifty characters required for validation purposes",
            Back = "Proposal back 1 with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        var proposal2 = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceTextId = sourceTextId,
            Front = "Proposal front 2 with minimum fifty characters required for validation purposes",
            Back = "Proposal back 2 with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        context.CardProposals.AddRange(proposal1, proposal2);
        await context.SaveChangesAsync();

        var request = new AcceptProposalsRequest(
            new[] { proposal1.Id, proposal2.Id },
            null
        );

        // Act
        var result = await service.AcceptProposalsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        var cards = await context.Cards.ToListAsync();
        Assert.Equal(2, cards.Count);
        Assert.All(cards, c =>
        {
            Assert.Equal(userId, c.UserId);
            Assert.Equal(GenerationMethod.Ai, c.GenerationMethod);
        });

        var progressRecords = await context.CardProgress.ToListAsync();
        Assert.Equal(2, progressRecords.Count);
        Assert.All(progressRecords, p =>
        {
            Assert.Equal(0, p.ReviewCount);
            Assert.Equal("{}", p.SrState);
        });

        var acceptanceEvents = await context.AcceptanceEvents.ToListAsync();
        Assert.Equal(2, acceptanceEvents.Count);
        Assert.All(acceptanceEvents, e => Assert.Equal(AcceptanceAction.Accepted, e.Action));

        var remainingProposals = await context.CardProposals.ToListAsync();
        Assert.Empty(remainingProposals);
    }

    [Fact]
    public async Task AcceptProposalsAsync_WithEdits_CreatesCardsWithEditedContent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceTextId = Guid.NewGuid();

        var proposal = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceTextId = sourceTextId,
            Front = "Original front with minimum fifty characters required for validation purposes",
            Back = "Original back with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        context.CardProposals.Add(proposal);
        await context.SaveChangesAsync();

        var edits = new Dictionary<Guid, EditedProposal>
        {
            { proposal.Id, new EditedProposal(
                "Edited front with minimum fifty characters required for validation purposes now",
                "Edited back with minimum fifty characters required for validation purposes now"
            )}
        };

        var request = new AcceptProposalsRequest(
            new[] { proposal.Id },
            edits
        );

        // Act
        var result = await service.AcceptProposalsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        
        var card = await context.Cards.FirstAsync();
        Assert.Equal("Edited front with minimum fifty characters required for validation purposes now", card.Front);
        Assert.Equal("Edited back with minimum fifty characters required for validation purposes now", card.Back);
    }

    [Fact]
    public async Task AcceptProposalsAsync_WrongUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var sourceTextId = Guid.NewGuid();

        var proposal = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            SourceTextId = sourceTextId,
            Front = "Proposal front with minimum fifty characters required for validation purposes",
            Back = "Proposal back with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        context.CardProposals.Add(proposal);
        await context.SaveChangesAsync();

        var request = new AcceptProposalsRequest(
            new[] { proposal.Id },
            null
        );

        // Act
        var result = await service.AcceptProposalsAsync(differentUserId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No proposals found", result.Error);
    }

    [Fact]
    public async Task RejectProposalsAsync_ValidProposals_RemovesProposalsAndCreatesEvents()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceTextId = Guid.NewGuid();

        var proposal1 = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceTextId = sourceTextId,
            Front = "Proposal front 1 with minimum fifty characters required for validation purposes",
            Back = "Proposal back 1 with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        var proposal2 = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceTextId = sourceTextId,
            Front = "Proposal front 2 with minimum fifty characters required for validation purposes",
            Back = "Proposal back 2 with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        context.CardProposals.AddRange(proposal1, proposal2);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RejectProposalsAsync(userId, new[] { proposal1.Id, proposal2.Id });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        var remainingProposals = await context.CardProposals.ToListAsync();
        Assert.Empty(remainingProposals);

        var acceptanceEvents = await context.AcceptanceEvents.ToListAsync();
        Assert.Equal(2, acceptanceEvents.Count);
        Assert.All(acceptanceEvents, e => Assert.Equal(AcceptanceAction.Rejected, e.Action));

        var cards = await context.Cards.ToListAsync();
        Assert.Empty(cards);
    }

    [Fact]
    public async Task RejectProposalsAsync_WrongUser_ReturnsFailure()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var sourceTextId = Guid.NewGuid();

        var proposal = new CardProposal
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            SourceTextId = sourceTextId,
            Front = "Proposal front with minimum fifty characters required for validation purposes",
            Back = "Proposal back with minimum fifty characters required for validation purposes",
            CreatedUtc = DateTime.UtcNow
        };

        context.CardProposals.Add(proposal);
        await context.SaveChangesAsync();

        // Act
        var result = await service.RejectProposalsAsync(differentUserId, new[] { proposal.Id });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No proposals found", result.Error);
    }

    [Fact]
    public async Task GenerateProposalsAsync_EnsuresFrontAndBackMeetMinimumLength()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        var sourceText = "Short sentence one. Short two. Short three. Short four. Short five. Short six.";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.True(result.IsSuccess);
        
        var proposals = await context.CardProposals.ToListAsync();
        Assert.All(proposals, p =>
        {
            Assert.True(p.Front.Length >= 50, $"Front length {p.Front.Length} is less than 50");
            Assert.True(p.Back.Length >= 50, $"Back length {p.Back.Length} is less than 50");
        });
    }

    [Fact]
    public async Task GenerateProposalsAsync_TruncatesLongContent()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new ProposalService(context);
        var userId = Guid.NewGuid();
        
        var longSentence = new string('a', 600);
        var sourceText = $"{longSentence}. Another sentence here. And one more sentence.";

        // Act
        var result = await service.GenerateProposalsAsync(userId, sourceText);

        // Assert
        Assert.True(result.IsSuccess);
        
        var proposals = await context.CardProposals.ToListAsync();
        Assert.All(proposals, p =>
        {
            Assert.True(p.Front.Length <= 500, $"Front length {p.Front.Length} exceeds 500");
            Assert.True(p.Back.Length <= 500, $"Back length {p.Back.Length} exceeds 500");
        });
    }
}
