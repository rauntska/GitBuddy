using Graphite.Api.BackgroundServices;
using Graphite.Api.Services;
using Graphite.Domain.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=graphite.db"));

builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddHostedService<PRRefreshService>();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowVueDev");

app.MapControllers();

app.Run();