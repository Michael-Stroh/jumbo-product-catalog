using Jumbo.ProductService.Domain.Configs;

namespace Jumbo.ProductService.Api.Configs;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Binds all strongly-typed options sections. Add new IOptions registrations here.
    /// </summary>
    public static IServiceCollection AddProjectOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));

        return services;
    }
}
