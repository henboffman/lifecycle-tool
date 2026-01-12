using LifecycleDashboard.Components;
using LifecycleDashboard.Data;
using LifecycleDashboard.Services;
using LifecycleDashboard.Services.DataIntegration;
using Microsoft.EntityFrameworkCore;

// Default to Development environment if not explicitly set
// This must be done BEFORE CreateBuilder so user secrets are loaded
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Data Protection for secure credential storage
builder.Services.AddDataProtection();

// Register database context (required for data persistence)
var connectionString = builder.Configuration.GetConnectionString("LifecycleDb");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'LifecycleDb' is required. " +
        "Please configure it in appsettings.json or appsettings.Development.json. " +
        "Run 'docker-compose up -d' to start the SQL Server container.");
}

// Use DbContextFactory for singleton services that need database access
builder.Services.AddDbContextFactory<LifecycleDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddTransient<DatabaseSeeder>();

// Register application services
builder.Services.AddSingleton<IReleaseNotesService, ReleaseNotesService>();
builder.Services.AddSingleton<IHealthScoringService, HealthScoringService>();

// Use database-backed data service for persistence
builder.Services.AddSingleton<IMockDataService, DatabaseDataService>();

// Register secure storage service
builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();

// Register audit service
builder.Services.AddSingleton<IAuditService, AuditService>();

// Register AI recommendation service
builder.Services.AddHttpClient<IAiRecommendationService, OllamaRecommendationService>();

// Register HttpClient for EOL data service
builder.Services.AddHttpClient<IEolDataService, EolDataService>();

// Register data integration services
builder.Services.AddHttpClient<IAzureDevOpsService, AzureDevOpsService>();
builder.Services.AddHttpClient<ISharePointService, SharePointService>();
builder.Services.AddSingleton<IServiceNowService, ServiceNowService>();
builder.Services.AddSingleton<IIisDatabaseService, IisDatabaseService>();
builder.Services.AddSingleton<IDataSyncOrchestrator, DataSyncOrchestrator>();

var app = builder.Build();

// Seed database with initial data if empty (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
else
{
    // In development, show detailed errors
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// Static files configuration
// UseStaticFiles provides fallback when MapStaticAssets manifest isn't available
// (e.g., running from bin folder, different machine, or without proper build)
app.UseStaticFiles();

// MapStaticAssets enables fingerprinted assets for cache busting in production
// This may fail silently if manifest isn't available, UseStaticFiles above provides fallback
try
{
    app.MapStaticAssets();
}
catch (InvalidOperationException)
{
    // Static asset manifest not available - UseStaticFiles fallback is already configured
    Console.WriteLine("Note: Static asset manifest not found. Using standard static file serving.");
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
