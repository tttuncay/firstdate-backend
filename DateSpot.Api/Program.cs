using Microsoft.EntityFrameworkCore;
using DateSpot.Application.Services;
using DateSpot.Core.Interfaces;
using DateSpot.Infrastructure.Data;
using DateSpot.Infrastructure.Repositories;
using DateSpot.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Cloud Run veya dış ortam Port desteği
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "First Date Mekan Önerici API", Version = "v1" });
});

// CORS Yapılandırması (Flutter mobil ve web için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// PostgreSQL + PostGIS DbContext Kaydı
var connectionString = builder.Configuration.GetConnectionString("PostgreSql") 
    ?? "Host=localhost;Port=5432;Database=datespot_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<DateSpotDbContext>(options =>
{
    options.UseNpgsql(connectionString, o => o.UseNetTopologySuite());
});

// HTTP Clients
builder.Services.AddHttpClient<IGeminiAdvisorService, GeminiAdvisorService>();
builder.Services.AddHttpClient<IRevenueCatService, RevenueCatService>();

// Dependency Injection
builder.Services.AddScoped<IVenueRepository, VenueRepository>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DateSpot AI API v1");
    c.RoutePrefix = string.Empty; // Doğrudan kök dizinde Swagger açılsın
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Otomatik Veritabanı Tohumlama (Seed Data)
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DateSpotDbContext>();
    await DbInitializer.SeedVenuesAsync(dbContext);
    Console.WriteLine("✅ İstanbul First Date mekanları veritabanına başarıyla yüklendi.");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Veritabanı bağlantısı henüz aktif değil veya tohumlama atlandı: {ex.Message}");
}

app.Run();
