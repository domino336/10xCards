using System;

namespace _10xCards.Domain.Entities;

public sealed class CardProgress
{
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTime NextReviewUtc { get; set; }
    public int ReviewCount { get; set; }
    public DateTime? LastReviewUtc { get; set; }
    public ReviewResult? LastReviewResult { get; set; }
    public string SrState { get; set; } = "{}";
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
