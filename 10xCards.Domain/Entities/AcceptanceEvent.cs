using System;

namespace _10xCards.Domain.Entities;

public sealed class AcceptanceEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid? CardId { get; set; }
    public AcceptanceAction Action { get; set; }
    public DateTime CreatedUtc { get; set; }
}
