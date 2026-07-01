using GitBuddy.Api.BackgroundServices;
using GitBuddy.Api.Filters;
using GitBuddy.Api.Hubs;
using GitBuddy.Api.Processors;
using GitBuddy.Api.Services;
using GitBuddy.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Octokit.Webhooks.AspNetCore;
using System.IO.Compression;
using System.Text;
using Octokit.Webhooks;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=GitBuddy;Username=gitbuddy;Password=gitbuddy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "GitBuddy",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "GitBuddyUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Core services
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddSingleton<INotificationService, SignalRNotificationService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<IAllowlistService, AllowlistService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// GitHub-related services
builder.Services.AddSingleton<IGitHubTokenService, GitHubTokenService>();
builder.Services.AddScoped<IGitHubGraphQLService, GitHubGraphQLService>();
builder.Services.AddScoped<IPullRequestStatusService, PullRequestStatusService>();
builder.Services.AddScoped<IRepositoryRuleService, RepositoryRuleService>();
builder.Services.AddScoped<IBranchWithoutPRService, BranchWithoutPRService>();
builder.Services.AddSingleton<IBranchWithoutPRRefreshTrigger, BranchWithoutPRRefreshTrigger>();

// Utility services
builder.Services.AddScoped<ILanguageDetectionService, LanguageDetectionService>();
builder.Services.AddScoped<IGitHubConfigValidationService, GitHubConfigValidationService>();
builder.Services.AddScoped<IPullRequestValidationService, PullRequestValidationService>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Webhook processing
builder.Services.AddScoped<WebhookEventProcessor, GitHubWebhookProcessor>();

// Background services
builder.Services.AddHostedService<PRRefreshService>();
builder.Services.AddHostedService<RepositoryRuleSyncWorker>();
builder.Services.AddHostedService<BranchWithoutPRRefreshService>();

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ApiExceptionFilterAttribute>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = new[]
    {
        "application/json",
        "text/json",
        "text/plain",
        "text/event-stream",
        "application/javascript",
        "text/css",
        "image/svg+xml"
    };
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseResponseCompression();
app.UseCors("AllowVueDev");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<PRHub>("/hubs/pr");
app.MapHealthChecks("/api/health");

var webhookSecret = builder.Configuration["GitHub:WebhookSecret"];
app.MapGitHubWebhooks(path: "/api/webhooks/github", secret: webhookSecret);

await app.RunAsync();