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
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Side).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CenterLocation).HasColumnType("geometry(Point, 4326)");
            entity.Property(e => e.PopularNeighborhoods)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.District).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Neighborhood).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);

            // PostGIS Coğrafi Nokta Tipi
            entity.Property(e => e.Location)
                  .HasColumnType("geometry(Point, 4326)");

            // JSON Serileştirilmiş Listeler
            entity.Property(e => e.CompatibleConcepts)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<DateConcept>>(v, (JsonSerializerOptions)null!) ?? new List<DateConcept>());

            entity.Property(e => e.VibeTags)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            entity.Property(e => e.GalleryImages)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>());

            // İndeksler
            entity.HasIndex(e => e.District);
            entity.HasIndex(e => e.PriceLevel);
            entity.HasIndex(e => e.FirstDateSuitabilityScore);
        });
    }
}
