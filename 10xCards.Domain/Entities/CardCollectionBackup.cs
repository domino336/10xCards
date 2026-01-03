namespace _10xCards.Domain.Entities;

public sealed class CardCollectionBackup
{
    public Guid CollectionId { get; set; }
    public CardCollection Collection { get; set; } = null!;
    public string PreviousName { get; set; } = string.Empty;
    public string PreviousDescription { get; set; } = string.Empty;
    public DateTime BackedUpUtc { get; set; }
}
