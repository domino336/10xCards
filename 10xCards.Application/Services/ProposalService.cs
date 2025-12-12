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

            // MOCK IMPLEMENTATION - Generate simple proposals from text
            // TODO: Replace with real OpenRouter API call in production
            var mockProposals = GenerateMockProposals(userId, sourceTextEntity.Id, sourceText);
            
            foreach (var proposal in mockProposals)
            {
                _context.CardProposals.Add(proposal);
            }
            
            await _context.SaveChangesAsync(cancellationToken);

            var proposalDtos = mockProposals.Select(p => new ProposalDto(
                p.Id,
                p.Front,
                p.Back,
                p.CreatedUtc
            )).ToArray();

            var response = new GenerateProposalsResponse(
                sourceTextEntity.Id,
                proposalDtos);

            return Result<GenerateProposalsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GenerateProposalsResponse>.Failure($"Failed to generate proposals: {ex.Message}");
        }
    }

    private List<CardProposal> GenerateMockProposals(Guid userId, Guid sourceTextId, string sourceText)
    {
        var proposals = new List<CardProposal>();
        var now = DateTime.UtcNow;

        // Split text into sentences
        var sentences = sourceText
            .Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 30)
            .Take(10)
            .ToList();

        // Generate proposals from sentences
        var proposalCount = Math.Min(sentences.Count / 2, 5); // Max 5 proposals

        for (int i = 0; i < proposalCount && i * 2 + 1 < sentences.Count; i++)
        {
            var sentence1 = sentences[i * 2];
            var sentence2 = i * 2 + 1 < sentences.Count ? sentences[i * 2 + 1] : sentence1;

            // Create question from first sentence
            var front = GenerateQuestion(sentence1, i);
            
            // Use second sentence as answer
            var back = sentence2.Length > 500 
                ? sentence2.Substring(0, 497) + "..." 
                : sentence2;

            // Ensure minimum length
            if (front.Length < 50)
                front = $"Based on the text: {front}. Please explain in detail.";
            
            if (back.Length < 50)
                back = $"According to the source material: {back}";

            proposals.Add(new CardProposal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceTextId = sourceTextId,
                Front = front.Length > 500 ? front.Substring(0, 497) + "..." : front,
                Back = back,
                CreatedUtc = now
            });
        }

        // If no proposals generated, create at least one generic proposal
        if (proposals.Count == 0)
        {
            var preview = sourceText.Length > 200 ? sourceText.Substring(0, 197) + "..." : sourceText;
            proposals.Add(new CardProposal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SourceTextId = sourceTextId,
                Front = "What are the main points covered in the following text? Provide a detailed summary.",
                Back = $"The text discusses: {preview}",
                CreatedUtc = now
            });
        }

        return proposals;
    }

    private string GenerateQuestion(string sentence, int index)
    {
        // Simple question templates based on sentence content
        var lowerSentence = sentence.ToLower();

        if (lowerSentence.Contains("is") || lowerSentence.Contains("are"))
        {
            return $"What {sentence.Split(new[] { " is ", " are " }, StringSplitOptions.None)[0].Trim()}?";
        }
        else if (lowerSentence.Contains("can") || lowerSentence.Contains("could"))
        {
            return $"Explain: {sentence}";
        }
        else if (lowerSentence.Contains("because") || lowerSentence.Contains("since"))
        {
            var parts = sentence.Split(new[] { "because", "since" }, StringSplitOptions.None);
            return $"Why {parts[0].Trim().ToLower()}?";
        }
        else
        {
            // Default template
            var words = sentence.Split(' ');
            var firstWords = string.Join(" ", words.Take(Math.Min(5, words.Length)));
            return $"What does the text say about '{firstWords}'?";
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
