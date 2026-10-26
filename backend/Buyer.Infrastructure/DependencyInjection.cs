using Buyer.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buyer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BuyerDatabase");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<CloneEbayDbContext>(options => options.UseSqlServer(connectionString));
        }

        return services;
    }
}
