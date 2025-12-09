using System.ComponentModel.DataAnnotations;

namespace _10xCards.Application.DTOs.Cards;

public sealed record CreateCardRequest(
    [StringLength(500, MinimumLength = 50, ErrorMessage = "Front must be between 50 and 500 characters")]
    string Front,
    [StringLength(500, MinimumLength = 50, ErrorMessage = "Back must be between 50 and 500 characters")]
    string Back);

public sealed record CardListItemDto(
    Guid Id,
    string FrontPreview,
    DateTime? NextReviewUtc,
    int ReviewCount,
    string StatusBadge);

public sealed record PagedCardsResponse(
    IReadOnlyList<CardListItemDto> Cards,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record ReviewCardDto(
    Guid Id,
    string Front,
    string Back);

public sealed record SessionSummaryDto(
    int TotalReviewed,
    int AgainCount,
    int GoodCount,
    int EasyCount);

public enum CardFilter
{
    All,
    DueToday,
    New
}
