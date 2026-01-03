using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using _10xCards.Domain.Entities;

namespace _10xCards.Persistance.Configurations;

public sealed class CardCollectionBackupConfiguration : IEntityTypeConfiguration<CardCollectionBackup>
{
    public void Configure(EntityTypeBuilder<CardCollectionBackup> builder)
    {
        builder.HasKey(x => x.CollectionId);

        builder.Property(x => x.PreviousName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.PreviousDescription)
            .HasMaxLength(1000);

        builder.HasOne(x => x.Collection)
            .WithOne()
            .HasForeignKey<CardCollectionBackup>(x => x.CollectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
