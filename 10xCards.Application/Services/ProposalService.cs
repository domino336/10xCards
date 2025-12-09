using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Proposals;
using _10xCards.Domain.Entities;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;

namespace _10xCards.Application.Services;

public class ProposalService : IProposalService
{
    private readonly CardsDbContext _context;

    public ProposalService(CardsDbContext context)
    {
        _context = context;
    }

    public async Task<Result<GenerateProposalsResponse>> GenerateProposalsAsync(
        Guid userId,
        string sourceText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceText) || sourceText.Length < 50 || sourceText.Length > 20000)
        {
            return Result<GenerateProposalsResponse>.Failure("Source text must be between 50 and 20,000 characters");
        }

        try
        {
            var sourceTextEntity = new SourceText
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = sourceText,
                CreatedUtc = DateTime.UtcNow
            };

            _context.SourceTexts.Add(sourceTextEntity);
            await _context.SaveChangesAsync(cancellationToken);

            // TODO: Implement AI generation
            // For now, return empty proposals list
            var response = new GenerateProposalsResponse(
                sourceTextEntity.Id,
                Array.Empty<ProposalDto>());

            return Result<GenerateProposalsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GenerateProposalsResponse>.Failure($"Failed to generate proposals: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<ProposalDetailDto>>> GetProposalsBySourceAsync(
        Guid userId,
        Guid sourceTextId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposals = await _context.CardProposals
                .Where(p => p.SourceTextId == sourceTextId && p.UserId == userId)
                .Select(p => new ProposalDetailDto(
                    p.Id,
                    p.Front,
                    p.Back,
                    p.CreatedUtc,
                    p.SourceTextId!.Value))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<ProposalDetailDto>>.Success(proposals);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<ProposalDetailDto>>.Failure($"Failed to get proposals: {ex.Message}");
        }
    }

    public async Task<Result<int>> AcceptProposalsAsync(
        Guid userId,
        AcceptProposalsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposals = await _context.CardProposals
                .Where(p => request.ProposalIds.Contains(p.Id) && p.UserId == userId)
                .ToListAsync(cancellationToken);

            if (!proposals.Any())
            {
                return Result<int>.Failure("No proposals found");
            }

            foreach (var proposal in proposals)
            {
                var front = proposal.Front;
                var back = proposal.Back;

                if (request.Edits != null && request.Edits.TryGetValue(proposal.Id, out var edit))
                {
                    front = edit.Front;
                    back = edit.Back;
                }

                var card = new Card
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Front = front,
                    Back = back,
                    GenerationMethod = GenerationMethod.Ai,
                    CreatedUtc = DateTime.UtcNow
                };

                var progress = new CardProgress
                {
                    CardId = card.Id,
                    ReviewCount = 0,
                    NextReviewUtc = DateTime.UtcNow,
                    SrState = "{}"
                };

                var acceptanceEvent = new AcceptanceEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProposalId = proposal.Id,
                    Action = AcceptanceAction.Accepted,
                    CreatedUtc = DateTime.UtcNow
                };

                _context.Cards.Add(card);
                _context.CardProgress.Add(progress);
                _context.AcceptanceEvents.Add(acceptanceEvent);
                _context.CardProposals.Remove(proposal);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(proposals.Count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to accept proposals: {ex.Message}");
        }
    }

    public async Task<Result<int>> RejectProposalsAsync(
        Guid userId,
        Guid[] proposalIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var proposals = await _context.CardProposals
                .Where(p => proposalIds.Contains(p.Id) && p.UserId == userId)
                .ToListAsync(cancellationToken);

            if (!proposals.Any())
            {
                return Result<int>.Failure("No proposals found");
            }

            foreach (var proposal in proposals)
            {
                var acceptanceEvent = new AcceptanceEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProposalId = proposal.Id,
                    Action = AcceptanceAction.Rejected,
                    CreatedUtc = DateTime.UtcNow
                };

                _context.AcceptanceEvents.Add(acceptanceEvent);
                _context.CardProposals.Remove(proposal);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(proposals.Count);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"Failed to reject proposals: {ex.Message}");
        }
    }
}
