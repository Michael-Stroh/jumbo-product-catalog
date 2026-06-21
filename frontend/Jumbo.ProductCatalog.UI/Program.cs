using Jumbo.ProductCatalog.UI.Components;
using Jumbo.ProductCatalog.UI.Configs;

var builder = WebApplication.CreateBuilder(args);

/*
    ==========================================
    Services
    ==========================================
*/

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddProjectOptions(builder.Configuration);

// Named HttpClient for server-side calls to the Product API.
var api = builder.Configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>()
    ?? throw new InvalidOperationException("Api options are not configured.");
builder.Services.AddHttpClient("ProductApi", c =>
    c.BaseAddress = new Uri(api.BaseUrl));

/*
    ==========================================
    Pipeline
    ==========================================
*/

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

/*
    ==========================================
    Endpoints
    ==========================================
*/

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
