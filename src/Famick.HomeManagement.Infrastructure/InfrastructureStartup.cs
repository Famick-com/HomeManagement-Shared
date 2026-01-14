using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using Famick.HomeManagement.Core.Interfaces;
using Famick.HomeManagement.Infrastructure.Data;
using Famick.HomeManagement.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Famick.HomeManagement.Infrastructure;

public static class InfrastructureStartup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Configure database context
        services.AddDbContext<HomeManagementDbContext>((serviceProvider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Famick.HomeManagement.Infrastructure");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
        });


        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Register data seeder
        services.AddScoped<DataSeeder>();

        // Register business services (from homemanagement-shared)
        services.AddScoped<IProductGroupService, ProductGroupService>();
        services.AddScoped<IShoppingLocationService, ShoppingLocationService>();
        services.AddScoped<IShoppingListService, ShoppingListService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IChoreService, ChoreService>();
        services.AddScoped<IProductsService, ProductsService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IHomeService, HomeService>();
        services.AddScoped<IEquipmentService, EquipmentService>();
        services.AddScoped<IStorageBinService, StorageBinService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<ITodoItemService, TodoItemService>();

        // Configure Geoapify address normalization service
        services.Configure<GeoapifyOptions>(configuration.GetSection(GeoapifyOptions.SectionName));
        services.AddHttpClient<IAddressNormalizationService, GeoapifyAddressService>();

        // Configure plugin system
        services.Configure<Plugins.PluginLoaderOptions>(options =>
        {
            options.PluginsPath = Path.Combine(environment.ContentRootPath, "plugins");
            options.LoadPluginsOnStartup = true;
        });


        // Register built-in plugins (order matters for pipeline - first registered runs first)
        services.AddSingleton<Core.Interfaces.Plugins.IPlugin,
            Plugins.Usda.UsdaFoodDataPlugin>();
        services.AddSingleton<Core.Interfaces.Plugins.IPlugin,
            Plugins.OpenFoodFacts.OpenFoodFactsPlugin>();
        services.AddSingleton<Core.Interfaces.Plugins.IPlugin,
            Plugins.Kroger.KrogerStorePlugin>();

        // Register plugin loader and lookup service
        services.AddSingleton<Core.Interfaces.Plugins.IPluginLoader,
            Plugins.PluginLoader>();
        services.AddScoped<IProductLookupService,
            ProductLookupService>();

        // Register store integration plugin system
        services.AddScoped<IStoreIntegrationService, StoreIntegrationService>();


        return services;
    }

    public static async Task ConfigureInfrastructure(this IHost app, IConfiguration configuration)
    {
        // Apply pending migrations on startup (configurable, default: true for self-hosted)
        var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate", true);
        if (autoMigrate)
        {
            using var migrationScope = app.Services.CreateScope();
            var dbContext = migrationScope.ServiceProvider.GetRequiredService<HomeManagementDbContext>();
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                Log.Information("Applying {Count} pending database migration(s)...", pendingMigrations.Count());
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully");
            }
        }

        // Seed default data for the fixed tenant
        using (var scope = app.Services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
            if (tenantProvider.TenantId.HasValue)
            {
                // Ensure tenant record exists
                var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
                await tenantService.EnsureTenantExistsAsync(tenantProvider.TenantId.Value);

                await seeder.SeedDefaultDataAsync(tenantProvider.TenantId.Value);
            }

            // Seed default equipment document tags
            var equipmentService = scope.ServiceProvider.GetRequiredService<IEquipmentService>();
            await equipmentService.SeedDefaultTagsAsync();
        }

        // Load plugins on startup
        var pluginLoader = app.Services.GetRequiredService<Core.Interfaces.Plugins.IPluginLoader>();
        await pluginLoader.LoadPluginsAsync();
    }
}
