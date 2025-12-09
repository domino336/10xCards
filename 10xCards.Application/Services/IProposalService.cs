using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Proposals;

namespace _10xCards.Application.Services;

public interface IProposalService
{
    Task<Result<GenerateProposalsResponse>> GenerateProposalsAsync(
        Guid userId,
        string sourceText,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProposalDetailDto>>> GetProposalsBySourceAsync(
        Guid userId,
        Guid sourceTextId,
        CancellationToken cancellationToken = default);

    Task<Result<int>> AcceptProposalsAsync(
        Guid userId,
        AcceptProposalsRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<int>> RejectProposalsAsync(
        Guid userId,
        Guid[] proposalIds,
        CancellationToken cancellationToken = default);
}
