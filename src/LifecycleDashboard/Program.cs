using LifecycleDashboard.Components;
using LifecycleDashboard.Services;
using LifecycleDashboard.Services.DataIntegration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Data Protection for secure credential storage
builder.Services.AddDataProtection();

// Register application services
builder.Services.AddSingleton<IReleaseNotesService, ReleaseNotesService>();
builder.Services.AddSingleton<IHealthScoringService, HealthScoringService>();
builder.Services.AddSingleton<IMockDataService, MockDataService>();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
