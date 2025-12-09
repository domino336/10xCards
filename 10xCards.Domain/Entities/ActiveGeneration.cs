using System;

namespace _10xCards.Domain.Entities;

public sealed class ActiveGeneration
{
    public Guid UserId { get; set; }
    public DateTime StartedUtc { get; set; }
}
