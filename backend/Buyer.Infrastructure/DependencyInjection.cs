using Buyer.Application.Interfaces.Addresses;
using Buyer.Application.Catalog;
using Buyer.Application.Interfaces.Authentication;
using Buyer.Application.Interfaces.Profile;
using Buyer.Application.Interfaces.Cart;
using Buyer.Infrastructure.Authentication;
using Buyer.Infrastructure.Catalog;
using Buyer.Infrastructure.Data;
using Buyer.Infrastructure.Repositories.Addresses;
using Buyer.Infrastructure.Data.Models;
using Buyer.Infrastructure.Repositories.Profile;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buyer.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

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
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
        }
        else
        {
            services.AddScoped<IProductRepository, UnavailableProductRepository>();
            services.AddScoped<IProductCatalogRepository, UnavailableProductRepository>();
            services.AddScoped<IAddressRepository, UnavailableAddressRepository>();
        }

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required")
            .Validate(options => options.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters")
            .Validate(options => options.DurationMinutes == 15, "Jwt:DurationMinutes must be 15")
            .ValidateOnStart();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();

        //PROFILE - EBAY-20
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICartService, CartService>();

        if (configuration.GetValue<string>("BuyerSchema") == "V3" && !string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<BuyerSchema.BuyerSchemaContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IUserRepository, BuyerSchema.UserRepository>();
            services.AddScoped<IProductRepository, BuyerSchema.ProductRepository>();
            services.AddScoped<IProductCatalogRepository, BuyerSchema.ProductCatalogRepository>();
            services.AddScoped<ICartRepository, BuyerSchema.CartRepository>();
            services.AddScoped<IAddressRepository, BuyerSchema.AddressRepository>();
            services.AddScoped<IProfileRepository, BuyerSchema.ProfileRepository>();
            services.AddScoped<BuyerSchema.BuyerWorkflowService>();
            services.AddScoped<BuyerSchema.AccountLifecycleService>();
        }

        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IListingCatalogRepository, Buyer.Infrastructure.Repositories.ListingCatalog.ListingCatalogRepository>();
        services.AddScoped<IListingCatalogService, ListingCatalogService>();
        services.Configure<Email.SmtpOptions>(configuration.GetSection(Email.SmtpOptions.SectionName));
        services.Configure<UrgencyBadgeOptions>(configuration.GetSection(UrgencyBadgeOptions.SectionName));
        services.AddScoped<IEmailSender, Email.SmtpEmailSender>();
        services.AddScoped<IEmailVerificationService, Email.EmailVerificationService>();
        return services;
    }
}
