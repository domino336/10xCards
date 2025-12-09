using System.ComponentModel.DataAnnotations;

namespace _10xCards.Application.DTOs.Proposals;

public sealed record GenerateProposalsRequest(
    [StringLength(20000, MinimumLength = 50, ErrorMessage = "Source text must be between 50 and 20,000 characters")]
    string SourceText);

public sealed record ProposalDto(
    Guid Id,
    string Front,
    string Back,
    DateTime CreatedUtc);

public sealed record ProposalDetailDto(
    Guid Id,
    string Front,
    string Back,
    DateTime CreatedUtc,
    Guid SourceTextId);

public sealed record GenerateProposalsResponse(
    Guid SourceTextId,
    IReadOnlyList<ProposalDto> Proposals);

public sealed record AcceptProposalsRequest(
    Guid[] ProposalIds,
    Dictionary<Guid, EditedProposal>? Edits);

public sealed record EditedProposal(
    [StringLength(500, MinimumLength = 50, ErrorMessage = "Front must be between 50 and 500 characters")]
    string Front,
    [StringLength(500, MinimumLength = 50, ErrorMessage = "Back must be between 50 and 500 characters")]
    string Back);
