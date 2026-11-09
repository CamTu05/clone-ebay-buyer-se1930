using Buyer.Application.Interfaces.Addresses;
using Buyer.Application.Interfaces.ListingCatalog;
using Buyer.Application.Interfaces.Authentication;
using Buyer.Application.Interfaces.Profile;
using Buyer.Application.Interfaces.Cart;
using Buyer.Infrastructure.Authentication;
using Buyer.Infrastructure.Repositories.ListingCatalog;
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
            services.AddScoped<IListingCatalogRepository, ListingCatalogRepository>();
            services.AddScoped<ICartRepository, CartRepository>();
            services.AddScoped<IAddressRepository, AddressRepository>();
        }
        else
        {
            services.AddScoped<IAddressRepository, UnavailableAddressRepository>();
        }

        services.AddScoped<IAddressService, AddressService>();
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
        services.AddScoped<IListingCatalogService, ListingCatalogService>();

        //PROFILE - EBAY-20
        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<ICartService, CartService>();

        // EMAIL VERIFICATION
        services.Configure<Buyer.Infrastructure.Email.SmtpOptions>(
            configuration.GetSection(Buyer.Infrastructure.Email.SmtpOptions.SectionName));
        services.AddScoped<Buyer.Application.Interfaces.Email.IEmailSender, Buyer.Infrastructure.Email.SmtpEmailSender>();
        services.AddScoped<Buyer.Application.Interfaces.Email.IEmailVerificationService, Buyer.Infrastructure.Email.EmailVerificationService>();

        return services;
    }
}
