using Jumbo.ProductService.Api;
using Jumbo.ProductService.Api.Configs;
using Jumbo.ProductService.Domain.Configs;
using Microsoft.AspNetCore.HttpLogging;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

/*
    ==========================================
    Services
    ==========================================
*/

builder.Services.AddProjectOptions(builder.Configuration);

// IOptions<T> is for injection into services. At registration time, bind a snapshot directly.
var cors = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(cors.AllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// MVC + API docs
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "Jumbo Product Service API",
            Version = "v1",
            Description = "REST API for managing products in the Jumbo product catalogue.",
            Contact = new()
            {
                Name = "Jumbo Product Service",
            },
        };
        return Task.CompletedTask;
    });
});

// Health + errors
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

// HTTP logging
builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.RequestMethod
        | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode
        | HttpLoggingFields.Duration;
});

/*
    ==========================================
    Pipeline
    ==========================================
*/

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Jumbo Product Service";
        options.Theme = ScalarTheme.Default;
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Observability
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpLogging();

// Error handling
app.UseExceptionHandler();
app.UseStatusCodePages();

// Security
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

/*
    ==========================================
    Endpoints
    ==========================================
*/

app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
