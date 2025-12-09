using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using _10xCards.Domain.Entities;
using _10xCards.Persistance.Configurations;


namespace _10xCards.Persistance;

public sealed class CardsDbContext : IdentityDbContext<IdentityUser>
{
    public CardsDbContext(DbContextOptions<CardsDbContext> options)
        : base(options)
    {
    }

    public DbSet<SourceText> SourceTexts => Set<SourceText>();
    public DbSet<CardProposal> CardProposals => Set<CardProposal>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardProgress> CardProgress => Set<CardProgress>();
    public DbSet<AcceptanceEvent> AcceptanceEvents => Set<AcceptanceEvent>();
    public DbSet<ActiveGeneration> ActiveGenerations => Set<ActiveGeneration>();
    public DbSet<GenerationError> GenerationErrors => Set<GenerationError>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ActiveGenerationConfiguration());
        modelBuilder.ApplyConfiguration(new CardProgressConfiguration());
        // ...other configurations
    }
}
