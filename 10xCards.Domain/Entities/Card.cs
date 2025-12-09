using System;

namespace _10xCards.Domain.Entities;

public sealed class Card
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SourceTextId { get; set; }
    public SourceText? SourceText { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public GenerationMethod GenerationMethod { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public CardProgress? Progress { get; set; }
}
