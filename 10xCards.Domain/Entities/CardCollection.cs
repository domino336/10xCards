namespace _10xCards.Domain.Entities;

public sealed class CardCollection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public ICollection<CardCollectionCard> CardCollectionCards { get; set; } = new List<CardCollectionCard>();
}
