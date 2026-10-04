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
    c.SwaggerDoc("v1", new() { Title = "TamYeri: Mekan Rehberi API", Version = "v1" });
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

// PostgreSQL / Supabase Connection String Çözücü
var rawConn = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("PostgreSql") 
    ?? "Host=localhost;Port=5432;Database=datespot_db;Username=postgres;Password=postgres";

var formattedConnectionString = ParsePostgreSqlConnectionString(rawConn);

builder.Services.AddDbContext<DateSpotDbContext>(options =>
{
    options.UseNpgsql(formattedConnectionString, o => o.UseNetTopologySuite());
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
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TamYeri API v1");
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
    
    const string initSql = @"
        CREATE EXTENSION IF NOT EXISTS postgis;

        CREATE TABLE IF NOT EXISTS ""Districts"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Name"" VARCHAR(100) NOT NULL UNIQUE,
            ""Side"" VARCHAR(50) NOT NULL,
            ""CenterLocation"" geometry(Point, 4326),
            ""PopularNeighborhoods"" TEXT NOT NULL DEFAULT '[]',
            ""TotalVenuesCount"" INT NOT NULL DEFAULT 0
        );

        CREATE TABLE IF NOT EXISTS ""Venues"" (
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""Name"" VARCHAR(200) NOT NULL,
            ""District"" VARCHAR(100) NOT NULL,
            ""Neighborhood"" VARCHAR(100),
            ""Address"" VARCHAR(500),
            ""Location"" geometry(Point, 4326),
            ""PriceLevel"" INT NOT NULL DEFAULT 0,
            ""NoiseLevel"" INT NOT NULL DEFAULT 0,
            ""CompatibleConcepts"" TEXT NOT NULL DEFAULT '[]',
            ""VibeTags"" TEXT NOT NULL DEFAULT '[]',
            ""SeatingArrangement"" TEXT,
            ""HasAlcohol"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""HasValetParking"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""RequiresReservation"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""HasOutdoorSeating"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""FirstDateSuitabilityScore"" DOUBLE PRECISION NOT NULL DEFAULT 0,
            ""GoogleRating"" DOUBLE PRECISION NOT NULL DEFAULT 0,
            ""ReviewCount"" INT NOT NULL DEFAULT 0,
            ""HeroImageUrl"" TEXT,
            ""GalleryImages"" TEXT NOT NULL DEFAULT '[]',
            ""GoogleMapsUrl"" TEXT,
            ""InstagramHandle"" TEXT,
            ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
        );

        CREATE INDEX IF NOT EXISTS ""IX_Venues_District"" ON ""Venues"" (""District"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_PriceLevel"" ON ""Venues"" (""PriceLevel"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_FirstDateSuitabilityScore"" ON ""Venues"" (""FirstDateSuitabilityScore"");
    ";

    await dbContext.Database.ExecuteSqlRawAsync(initSql);
    await DbInitializer.SeedVenuesAsync(dbContext);
    Console.WriteLine("✅ İstanbul TamYeri veritabanı tabloları ve ilçeleri Supabase'de başarıyla hazırlandı!");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Veritabanı bağlantı/tablo oluşturma detayı: {ex.Message}");
}

app.Run();

static string ParsePostgreSqlConnectionString(string input)
{
    if (string.IsNullOrWhiteSpace(input)) return input;

    // Eğer postgresql:// veya postgres:// formatında URI ise Npgsql formatına çevir
    if (input.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) || 
        input.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(input);
            var userInfo = uri.UserInfo.Split(':');
            var user = userInfo.Length > 0 ? userInfo[0] : "postgres";
            var pass = userInfo.Length > 1 ? userInfo[1] : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var db = uri.AbsolutePath.TrimStart('/');
            if (string.IsNullOrEmpty(db)) db = "postgres";

            return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=15;";
        }
        catch
        {
            return input;
        }
    }

    // Zaten Host= formatındaysa ve SSL içermiyorsa ekle
    if (!input.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) && !input.Contains("localhost", StringComparison.OrdinalIgnoreCase))
    {
        input += ";SSL Mode=Require;Trust Server Certificate=true;Timeout=15;Command Timeout=15;";
    }

    return input;
}
