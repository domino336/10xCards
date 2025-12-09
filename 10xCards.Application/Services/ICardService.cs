using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Cards;

namespace _10xCards.Application.Services;

public interface ICardService
{
    Task<Result<Guid>> CreateManualCardAsync(
        Guid userId,
        string front,
        string back,
        CancellationToken cancellationToken = default);

    Task<Result<PagedCardsResponse>> GetCardsAsync(
        Guid userId,
        CardFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateCardAsync(
        Guid cardId,
        Guid userId,
        string front,
        string back,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteCardAsync(
        Guid cardId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ReviewCardDto>>> GetDueCardsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalCardsAsync(CancellationToken cancellationToken = default);
}
