using System;

namespace _10xCards.Domain.Entities;

public sealed class SourceText
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
