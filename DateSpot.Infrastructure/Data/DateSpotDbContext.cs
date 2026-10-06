using Microsoft.EntityFrameworkCore;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;
using System.Text.Json;

namespace DateSpot.Infrastructure.Data;

public class DateSpotDbContext : DbContext
{
    public DateSpotDbContext(DbContextOptions<DateSpotDbContext> options) : base(options)
    {
    }

    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<District> Districts => Set<District>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL PostGIS Uzantısı
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<District>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Country).HasMaxLength(100).HasDefaultValue("Türkiye");
            entity.Property(e => e.CountryCode).HasMaxLength(10).HasDefaultValue("TR");
            entity.Property(e => e.City).HasMaxLength(100).HasDefaultValue("İstanbul");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StateOrRegion).HasMaxLength(100);
            entity.Property(e => e.Zone).HasMaxLength(100).HasDefaultValue(string.Empty);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Ignore(e => e.Side); // Side is a computed compatibility wrapper for Zone
            entity.Property(e => e.PopularNeighborhoods)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());
            
            entity.HasIndex(e => new { e.CountryCode, e.City, e.Name });
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.CountryCode);
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GooglePlaceId).HasMaxLength(255);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Country).HasMaxLength(100).HasDefaultValue("Türkiye");
            entity.Property(e => e.CountryCode).HasMaxLength(10).HasDefaultValue("TR");
            entity.Property(e => e.City).HasMaxLength(100).HasDefaultValue("İstanbul");
            entity.Property(e => e.StateOrRegion).HasMaxLength(100);
            entity.Property(e => e.District).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Neighborhood).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Currency).HasMaxLength(10).HasDefaultValue("TRY");
            entity.Property(e => e.TimeZone).HasMaxLength(50).HasDefaultValue("Europe/Istanbul");

            // Foreign Key İlişkisi
            entity.HasOne(v => v.DistrictRef)
                  .WithMany()
                  .HasForeignKey(v => v.DistrictId)
                  .OnDelete(DeleteBehavior.SetNull);

            // JSON Serileştirilmiş Koleksiyonlar
            entity.Property(e => e.GalleryImages)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.ViewType)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.CuisineTypes)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.MealTimes)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.SignatureItems)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.DietaryOptions)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.SeatingTypes)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.SuitableOccasions)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.VibeTags)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            // İndeksler
            entity.HasIndex(e => e.GooglePlaceId).IsUnique();
            entity.HasIndex(e => e.DistrictId);
            entity.HasIndex(e => e.Country);
            entity.HasIndex(e => e.CountryCode);
            entity.HasIndex(e => e.City);
            entity.HasIndex(e => e.District);
            entity.HasIndex(e => e.PriceLevel);
            entity.HasIndex(e => e.NoiseLevel);
            entity.HasIndex(e => e.GoogleRating);
            entity.HasIndex(e => e.FirstDateSuitabilityScore);
        });
    }
}
