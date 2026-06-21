using Jumbo.ProductCatalog.Core.Interfaces;
using Jumbo.ProductCatalog.Core.Services;
using Jumbo.ProductCatalog.Domain.Configs;
using Jumbo.ProductCatalog.Infrastructure.Data;
using Jumbo.ProductCatalog.Infrastructure.Data.Interceptors;
using Jumbo.ProductCatalog.Infrastructure.Repositories;
using Jumbo.ProductCatalog.Infrastructure.Services;
using Jumbo.ProductCatalog.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

/*
    ==========================================
    Services
    ==========================================
*/

builder.Services.Configure<BlobStorageConfig>(builder.Configuration.GetSection(BlobStorageConfig.SectionName));
builder.Services.Configure<DatabaseConfig>(builder.Configuration.GetSection(DatabaseConfig.SectionName));
builder.Services.Configure<ExportConfig>(builder.Configuration.GetSection(ExportConfig.SectionName));

var connectionString = builder.Configuration.GetSection(DatabaseConfig.SectionName)[nameof(DatabaseConfig.ConnectionString)]
    ?? throw new InvalidOperationException($"Missing required configuration '{DatabaseConfig.SectionName}:{nameof(DatabaseConfig.ConnectionString)}'.");

builder.Services.AddSingleton<UpdateTimestampsInterceptor>();

builder.Services.AddDbContext<ProductDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<UpdateTimestampsInterceptor>()));

builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IFileStorageService, BlobFileStorageService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddHostedService<Worker>();

/*
    ==========================================
    Run
    ==========================================
*/

var host = builder.Build();
host.Run();
