using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;
using DateSpot.Core.Interfaces;
using DateSpot.Infrastructure.Data;

namespace DateSpot.Infrastructure.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly DateSpotDbContext _context;
    private readonly GeometryFactory _geometryFactory;

    public VenueRepository(DateSpotDbContext context)
    {
        _context = context;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    public async Task<List<Venue>> GetVenuesByFiltersAsync(
        string? district,
        List<string>? coveredDistricts,
        double? userLat,
        double? userLng,
        double radiusInKm,
        PriceLevel? maxPriceLevel,
        bool? requiresAlcohol,
        bool? requiresParking,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Venues.AsNoTracking().AsQueryable();

        // 1. Çoklu Kapsanan İlçeler Filtresi
        if (coveredDistricts != null && coveredDistricts.Any())
        {
            var lowerDistricts = coveredDistricts.Select(d => d.ToLower()).ToList();
            query = query.Where(v => lowerDistricts.Contains(v.District.ToLower()));
        }
        else if (!string.IsNullOrWhiteSpace(district) && 
            !district.Equals("Tüm İstanbul", StringComparison.OrdinalIgnoreCase) &&
            !district.Equals("Hepsi", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(v => v.District.ToLower() == district.ToLower());
        }

        // 2. PostGIS Konum / Yarıçap Filtresi (Kullanıcı koordinat vermişse)
        if (userLat.HasValue && userLng.HasValue)
        {
            var userPoint = _geometryFactory.CreatePoint(new Coordinate(userLng.Value, userLat.Value));
            // Yaklaşık derece cinsinden mesafe (1 derece ~ 111 km)
            double radiusInDegrees = radiusInKm / 111.0;
            query = query.Where(v => v.Location.IsWithinDistance(userPoint, radiusInDegrees));
        }

        // 3. Bütçe Filtresi
        if (maxPriceLevel.HasValue)
        {
            query = query.Where(v => v.PriceLevel <= maxPriceLevel.Value);
        }

        // 4. Alkol Filtresi
        if (requiresAlcohol.HasValue && requiresAlcohol.Value)
        {
            query = query.Where(v => v.HasAlcohol);
        }

        // 5. Otopark Filtresi
        if (requiresParking.HasValue && requiresParking.Value)
        {
            query = query.Where(v => v.HasValetParking);
        }

        return await query.Take(40).ToListAsync(cancellationToken);
    }

    public async Task<List<Venue>> GetAllVenuesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Venues.AsNoTracking().Take(50).ToListAsync(cancellationToken);
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Venue> venues, CancellationToken cancellationToken = default)
    {
        await _context.Venues.AddRangeAsync(venues, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Venues.CountAsync(cancellationToken);
    }
}
