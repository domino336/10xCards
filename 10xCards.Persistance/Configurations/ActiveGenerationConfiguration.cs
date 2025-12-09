using _10xCards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace _10xCards.Persistance.Configurations;

public sealed class ActiveGenerationConfiguration : IEntityTypeConfiguration<ActiveGeneration>
{
    public void Configure(EntityTypeBuilder<ActiveGeneration> builder)
    {
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.StartedUtc).IsRequired();
    }
}
