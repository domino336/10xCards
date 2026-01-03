using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using _10xCards.Domain.Entities;

namespace _10xCards.Persistance.Configurations;

public sealed class CardCollectionConfiguration : IEntityTypeConfiguration<CardCollection>
{
    public void Configure(EntityTypeBuilder<CardCollection> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.CreatedUtc);
    }
}
