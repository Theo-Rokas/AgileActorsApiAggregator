using System.Net.Http.Headers;
using System.Text;
using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Infrastructure.Clients;
using AgileActorsApiAggregator.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Memory cache ─────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Stats store (singleton so it lives for the app lifetime) ─────────────────
builder.Services.AddSingleton<InMemoryStatsStore>();
builder.Services.AddSingleton<IStatsStore>(sp => sp.GetRequiredService<InMemoryStatsStore>());

// ── Refit HTTP clients ────────────────────────────────────────────────────────
builder.Services.AddRefitClient<IWeatherApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openweathermap.org"));

builder.Services.AddRefitClient<INewsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://newsapi.org"));

var githubToken = builder.Configuration["ExternalApis:GitHub:Token"];
builder.Services.AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri("https://api.github.com");
        if (!string.IsNullOrWhiteSpace(githubToken))
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
    });

builder.Services.AddScoped<IWeatherClient, WeatherClient>();
builder.Services.AddScoped<INewsClient, NewsClient>();
builder.Services.AddScoped<IGitHubClient, GitHubClient>();

// ── Aggregation service ───────────────────────────────────────────────────────
builder.Services.AddScoped<IAggregationService, AggregationService>();

// ── Background performance monitor ───────────────────────────────────────────
builder.Services.AddHostedService<PerformanceMonitorService>();

// ── JWT authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── Swagger with JWT bearer support ──────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Aggregator", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", doc), [] }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Needed for WebApplicationFactory in integration tests
public partial class Program { }
