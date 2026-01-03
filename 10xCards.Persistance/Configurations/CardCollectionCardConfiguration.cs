using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using _10xCards.Domain.Entities;

namespace _10xCards.Persistance.Configurations;

public sealed class CardCollectionCardConfiguration : IEntityTypeConfiguration<CardCollectionCard>
{
    public void Configure(EntityTypeBuilder<CardCollectionCard> builder)
    {
        builder.HasKey(x => new { x.CollectionId, x.CardId });

        builder.HasOne(x => x.Collection)
            .WithMany(c => c.CardCollectionCards)
            .HasForeignKey(x => x.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Card)
            .WithMany()
            .HasForeignKey(x => x.CardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CollectionId);
        builder.HasIndex(x => x.CardId);
    }
}
