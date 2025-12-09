using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace _10xCards.DesignTime;

public class CardsDbContextFactory : IDesignTimeDbContextFactory<CardsDbContext>
{
    public CardsDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<CardsDbContext>();
        builder.UseSqlite("Data Source=cards.db");
        return new CardsDbContext(builder.Options);
    }
}