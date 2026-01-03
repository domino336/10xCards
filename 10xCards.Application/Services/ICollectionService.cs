using _10xCards.Application.Common;
using _10xCards.Application.DTOs.Collections;

namespace _10xCards.Application.Services;

public interface ICollectionService
{
    Task<Result<Guid>> CreateCollectionAsync(
        Guid userId,
        string name,
        string description,
        List<Guid> cardIds,
        CancellationToken cancellationToken = default);

    Task<Result<CollectionDetailDto>> GetCollectionAsync(
        Guid collectionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedCollectionsResponse>> GetCollectionsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateCollectionAsync(
        Guid collectionId,
        Guid userId,
        string name,
        string description,
        CancellationToken cancellationToken = default);

    Task<Result> AddCardsToCollectionAsync(
        Guid collectionId,
        Guid userId,
        List<Guid> cardIds,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveCardsFromCollectionAsync(
        Guid collectionId,
        Guid userId,
        List<Guid> cardIds,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteCollectionAsync(
        Guid collectionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result> RestorePreviousVersionAsync(
        Guid collectionId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
