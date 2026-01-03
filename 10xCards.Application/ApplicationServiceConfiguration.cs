using _10xCards.Application.Services;
using _10xCards.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace _10xCards.Application
{
    public static class ApplicationServiceConfiguration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ICardService, CardService>();
            services.AddScoped<IProposalService, ProposalService>();
            services.AddScoped<ISrService, SrService>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<ICollectionService, CollectionService>();
            return services;
        }
    }
}