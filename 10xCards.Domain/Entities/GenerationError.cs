using System;

namespace _10xCards.Domain.Entities;

public sealed class GenerationError
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SourceTextId { get; set; }
    public string ErrorType { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedUtc { get; set; }
}
