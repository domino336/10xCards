namespace _10xCards.Application.DTOs.Collections;

public sealed record CollectionDto(
    Guid Id,
    string Name,
    string Description,
    int CardCount,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    bool HasBackup
);

public sealed record CollectionDetailDto(
    Guid Id,
    string Name,
    string Description,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    bool HasBackup,
    List<CollectionCardDto> Cards
);

public sealed record CollectionCardDto(
    Guid CardId,
    string Front,
    string Back,
    DateTime AddedUtc
);

public sealed record PagedCollectionsResponse(
    List<CollectionDto> Collections,
    int TotalCount,
    int Page,
    int PageSize
);

public sealed record CreateCollectionRequest(
    string Name,
    string Description,
    List<Guid> CardIds
);

public sealed record UpdateCollectionRequest(
    string Name,
    string Description
);

public sealed record AddCardsToCollectionRequest(
    List<Guid> CardIds
);
