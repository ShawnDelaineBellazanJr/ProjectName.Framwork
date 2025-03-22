using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.AIServices;
using ProjectName.Domain.Interfaces;
using ProjectName.Infrastructure.Logging;
using ProjectName.Infrastructure.Repositories;


namespace ProjectName.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache(); 
            services.AddSingleton<IWisdomLogger, WisdomLogger>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
