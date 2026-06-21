using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Domain.Configs;
using Jumbo.ProductCatalog.Infrastructure.Data;
using Jumbo.ProductCatalog.Infrastructure.Data.Interceptors;
using Jumbo.ProductCatalog.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Jumbo.ProductCatalog.Api.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IProductCatalogService, Core.Services.ProductCatalogService>();

        services.AddOutputCache();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetSection(DatabaseConfig.SectionName)[nameof(DatabaseConfig.ConnectionString)]
            ?? throw new InvalidOperationException($"Missing required configuration '{DatabaseConfig.SectionName}:{nameof(DatabaseConfig.ConnectionString)}'.");

        services.AddSingleton<UpdateTimestampsInterceptor>();

        services.AddDbContext<ProductDbContext>((sp, options) =>
            options.UseSqlServer(connectionString)
                   .AddInterceptors(sp.GetRequiredService<UpdateTimestampsInterceptor>()));

        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
