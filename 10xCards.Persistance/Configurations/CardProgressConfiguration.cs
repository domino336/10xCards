using _10xCards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _10xCards.Persistance.Configurations;

public sealed class CardProgressConfiguration : IEntityTypeConfiguration<CardProgress>
{
    public void Configure(EntityTypeBuilder<CardProgress> builder)
    {
        builder.HasKey(x => x.CardId);
        builder.Property(x => x.SrState).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.NextReviewUtc });
        // Relacja 1:1 z Card
        builder.HasOne(x => x.Card)
            .WithOne(x => x.Progress)
            .HasForeignKey<CardProgress>(x => x.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
