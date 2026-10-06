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
        CREATE TABLE IF NOT EXISTS ""Districts"" (
            ""Id"" SERIAL PRIMARY KEY,
            ""Country"" VARCHAR(100) NOT NULL DEFAULT 'Türkiye',
            ""CountryCode"" VARCHAR(10) NOT NULL DEFAULT 'TR',
            ""City"" VARCHAR(100) NOT NULL DEFAULT 'İstanbul',
            ""StateOrRegion"" VARCHAR(100) DEFAULT '',
            ""Name"" VARCHAR(100) NOT NULL,
            ""Zone"" VARCHAR(100) NOT NULL DEFAULT '',
            ""Side"" VARCHAR(50) DEFAULT '',
            ""Latitude"" DOUBLE PRECISION NOT NULL DEFAULT 0.0,
            ""Longitude"" DOUBLE PRECISION NOT NULL DEFAULT 0.0,
            ""PopularNeighborhoods"" TEXT NOT NULL DEFAULT '[]',
            ""TotalVenuesCount"" INT NOT NULL DEFAULT 0,
            ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE
        );

        CREATE TABLE IF NOT EXISTS ""Venues"" (
            -- 1. Kimlik & Lokasyon (Global)
            ""Id"" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            ""DistrictId"" INT REFERENCES ""Districts""(""Id"") ON DELETE SET NULL,
            ""Name"" VARCHAR(200) NOT NULL,
            ""Country"" VARCHAR(100) NOT NULL DEFAULT 'Türkiye',
            ""CountryCode"" VARCHAR(10) NOT NULL DEFAULT 'TR',
            ""City"" VARCHAR(100) NOT NULL DEFAULT 'İstanbul',
            ""StateOrRegion"" VARCHAR(100) DEFAULT '',
            ""District"" VARCHAR(100) NOT NULL,
            ""Neighborhood"" VARCHAR(100),
            ""Address"" VARCHAR(500) NOT NULL,
            ""PostalCode"" VARCHAR(20) DEFAULT '',
            ""Currency"" VARCHAR(10) NOT NULL DEFAULT 'TRY',
            ""TimeZone"" VARCHAR(50) NOT NULL DEFAULT 'Europe/Istanbul',
            ""GoogleRating"" DOUBLE PRECISION NOT NULL DEFAULT 4.5,
            ""ReviewCount"" INT NOT NULL DEFAULT 100,
            ""HeroImageUrl"" TEXT DEFAULT '',
            ""GalleryImages"" TEXT NOT NULL DEFAULT '[]',
            ""GoogleMapsUrl"" TEXT DEFAULT '',
            ""InstagramHandle"" VARCHAR(100) DEFAULT '',
            ""WebsiteUrl"" VARCHAR(300),
            ""PhoneNumber"" VARCHAR(50),

            -- 2. Ambiyans & Duyusal Profil
            ""NoiseLevel"" INT NOT NULL DEFAULT 2,
            ""LightingStyle"" VARCHAR(100) NOT NULL DEFAULT 'Sıcak Sarı',
            ""MusicProfile"" TEXT NOT NULL DEFAULT 'Caz & Akustik',
            ""DressCode"" VARCHAR(100) NOT NULL DEFAULT 'Casual',
            ""ViewType"" TEXT NOT NULL DEFAULT '[]',

            -- 3. Mutfak, Menü & İmza Lezzetler
            ""PriceLevel"" INT NOT NULL DEFAULT 2,
            ""CuisineTypes"" TEXT NOT NULL DEFAULT '[]',
            ""MealTimes"" TEXT NOT NULL DEFAULT '[]',
            ""SignatureItems"" TEXT NOT NULL DEFAULT '[]',
            ""DietaryOptions"" TEXT NOT NULL DEFAULT '[]',

            -- 4. Masa, Oturma & Mekansal Taktikler
            ""SeatingTypes"" TEXT NOT NULL DEFAULT '[]',
            ""TableSpacing"" VARCHAR(100) NOT NULL DEFAULT 'Ferah',
            ""BestTableTip"" TEXT DEFAULT '',
            ""SeatingArrangement"" TEXT,

            -- 5. Pratik Kolaylıklar
            ""HasAlcohol"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""HasOutdoorSeating"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""HasValetParking"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""RequiresReservation"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""IsPetFriendly"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""HasWifiAndSockets"" BOOLEAN NOT NULL DEFAULT FALSE,
            ""SmokingArea"" VARCHAR(100) DEFAULT 'Bahçe',

            -- 6. AI & Buluşma Amaçları
            ""SuitableOccasions"" TEXT NOT NULL DEFAULT '[]',
            ""VibeTags"" TEXT NOT NULL DEFAULT '[]',
            ""BestTimeToVisit"" VARCHAR(200) DEFAULT '',
            ""FirstDateSuitabilityScore"" DOUBLE PRECISION NOT NULL DEFAULT 8.5,
            ""RawMetadata"" TEXT NOT NULL DEFAULT '{}',
            ""CreatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
        );

        -- Mevcut tablolar için yeni global lokasyon ve optimizasyon kolonlarını güvenle ekle
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""Country"" VARCHAR(100) NOT NULL DEFAULT 'Türkiye';
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""CountryCode"" VARCHAR(10) NOT NULL DEFAULT 'TR';
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""City"" VARCHAR(100) NOT NULL DEFAULT 'İstanbul';
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""StateOrRegion"" VARCHAR(100) DEFAULT '';
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""Zone"" VARCHAR(100) NOT NULL DEFAULT '';
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""Latitude"" DOUBLE PRECISION NOT NULL DEFAULT 0.0;
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""Longitude"" DOUBLE PRECISION NOT NULL DEFAULT 0.0;
        ALTER TABLE ""Districts"" ADD COLUMN IF NOT EXISTS ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE;

        -- Eski Side verisini Zone'a aktar (Geriye dönük uyumluluk)
        UPDATE ""Districts"" SET ""Zone"" = ""Side"" WHERE (""Zone"" = '' OR ""Zone"" IS NULL) AND ""Side"" IS NOT NULL AND ""Side"" <> '';

        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""DistrictId"" INT;
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""Country"" VARCHAR(100) NOT NULL DEFAULT 'Türkiye';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""CountryCode"" VARCHAR(10) NOT NULL DEFAULT 'TR';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""City"" VARCHAR(100) NOT NULL DEFAULT 'İstanbul';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""StateOrRegion"" VARCHAR(100) DEFAULT '';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""PostalCode"" VARCHAR(20) DEFAULT '';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""Latitude"" DOUBLE PRECISION;
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""Longitude"" DOUBLE PRECISION;
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""Currency"" VARCHAR(10) NOT NULL DEFAULT 'TRY';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""TimeZone"" VARCHAR(50) NOT NULL DEFAULT 'Europe/Istanbul';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""WebsiteUrl"" VARCHAR(300);
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""PhoneNumber"" VARCHAR(50);
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""LightingStyle"" VARCHAR(100) NOT NULL DEFAULT 'Sıcak Sarı';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""MusicProfile"" TEXT NOT NULL DEFAULT 'Caz & Akustik';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""DressCode"" VARCHAR(100) NOT NULL DEFAULT 'Casual';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""ViewType"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""CuisineTypes"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""MealTimes"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""SignatureItems"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""DietaryOptions"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""SeatingTypes"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""TableSpacing"" VARCHAR(100) NOT NULL DEFAULT 'Ferah';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""BestTableTip"" TEXT DEFAULT '';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""IsPetFriendly"" BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""HasWifiAndSockets"" BOOLEAN NOT NULL DEFAULT FALSE;
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""SmokingArea"" VARCHAR(100) DEFAULT 'Bahçe';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""SuitableOccasions"" TEXT NOT NULL DEFAULT '[]';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""BestTimeToVisit"" VARCHAR(200) DEFAULT '';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""OpeningHours"" TEXT NOT NULL DEFAULT '{}';
        ALTER TABLE ""Venues"" ADD COLUMN IF NOT EXISTS ""RawMetadata"" TEXT NOT NULL DEFAULT '{}';

        -- Mevcut mekanların DistrictId, Country ve City alanlarını otomatik doldur
        UPDATE ""Venues"" SET ""Country"" = 'Türkiye' WHERE ""Country"" IS NULL OR ""Country"" = '';
        UPDATE ""Venues"" SET ""CountryCode"" = 'TR' WHERE ""CountryCode"" IS NULL OR ""CountryCode"" = '';
        UPDATE ""Venues"" SET ""City"" = 'İstanbul' WHERE ""City"" IS NULL OR ""City"" = '';

        UPDATE ""Venues"" v
        SET ""DistrictId"" = d.""Id""
        FROM ""Districts"" d
        WHERE LOWER(v.""District"") = LOWER(d.""Name"")
          AND v.""DistrictId"" IS NULL;

        CREATE INDEX IF NOT EXISTS ""IX_Venues_DistrictId"" ON ""Venues"" (""DistrictId"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_Country"" ON ""Venues"" (""Country"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_CountryCode"" ON ""Venues"" (""CountryCode"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_City"" ON ""Venues"" (""City"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_District"" ON ""Venues"" (""District"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_PriceLevel"" ON ""Venues"" (""PriceLevel"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_NoiseLevel"" ON ""Venues"" (""NoiseLevel"");
        CREATE INDEX IF NOT EXISTS ""IX_Venues_GoogleRating"" ON ""Venues"" (""GoogleRating"" DESC);
        CREATE INDEX IF NOT EXISTS ""IX_Venues_FirstDateSuitabilityScore"" ON ""Venues"" (""FirstDateSuitabilityScore"");
    ";

    var conn = dbContext.Database.GetDbConnection();
    if (conn.State != System.Data.ConnectionState.Open)
    {
        await conn.OpenAsync();
    }
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = initSql;
        await cmd.ExecuteNonQueryAsync();
    }

    await DbInitializer.SeedVenuesAsync(dbContext);
    Console.WriteLine("✅ Optimize edilmiş Global TamYeri veritabanı tabloları ve ilçeleri Supabase'de başarıyla hazırlandı!");
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
