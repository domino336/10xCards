using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace _10xCards.Persistance
{
    public static class ApplicationServiceConfiguration
    {
        public static IServiceCollection AddPersistance(this IServiceCollection services)
        {
            services.AddDbContext<CardsDbContext>(options =>
                options.UseSqlite("Data Source=cards.db"));
            return services;
        }
    }
}