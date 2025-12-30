using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Famick.HomeManagement.Core;

public static class CoreStartup
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        // Register version service
        services.AddSingleton<IVersionService, VersionService>();

        return services;
    }
}