namespace _10xCards.Domain.Entities;

public sealed class CardCollectionCard
{
    public Guid CollectionId { get; set; }
    public CardCollection Collection { get; set; } = null!;
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public DateTime AddedUtc { get; set; }
}
