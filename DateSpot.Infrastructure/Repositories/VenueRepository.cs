using Microsoft.EntityFrameworkCore;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;
using DateSpot.Core.Interfaces;
using DateSpot.Infrastructure.Data;

namespace DateSpot.Infrastructure.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly DateSpotDbContext _context;

    public VenueRepository(DateSpotDbContext context)
    {
        _context = context;
    }

    public async Task<List<Venue>> GetVenuesByFiltersAsync(
        string? country,
        string? city,
        string? district,
        List<string>? coveredDistricts,
        PriceLevel? maxPriceLevel,
        bool? requiresAlcohol,
        bool? requiresParking,
        bool? requiresOutdoor = null,
        string? occasion = null,
        string? venueType = null,
        int candidateLimit = 150,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Venues.AsNoTracking().AsQueryable();

            // 1. Ülke Filtresi (Opsiyonel)
            if (!string.IsNullOrWhiteSpace(country) && 
                !country.Equals("Tümü", StringComparison.OrdinalIgnoreCase) && 
                !country.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !country.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => v.Country.ToLower() == country.ToLower() || v.CountryCode.ToLower() == country.ToLower());
            }

            // 2. Şehir Filtresi (Opsiyonel)
            if (!string.IsNullOrWhiteSpace(city) && 
                !city.Equals("Tümü", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("Tüm Şehir", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("All Cities", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => v.City.ToLower() == city.ToLower());
            }

            // 3. İlçe / Bölge Filtresi (CoveredDistricts veya Tek İlçe)
            if (coveredDistricts != null && coveredDistricts.Any())
            {
                var lowerDistricts = coveredDistricts.Select(d => d.ToLower()).ToList();
                query = query.Where(v => lowerDistricts.Contains(v.District.ToLower()));
            }
            else if (!string.IsNullOrWhiteSpace(district) && 
                !district.Equals("Tüm İstanbul", StringComparison.OrdinalIgnoreCase) &&
                !district.Equals("Tüm Şehir", StringComparison.OrdinalIgnoreCase) &&
                !district.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) &&
                !district.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(v => v.District.ToLower() == district.ToLower());
            }

            // 4. Bütçe Filtresi
            if (maxPriceLevel.HasValue)
            {
                query = query.Where(v => v.PriceLevel <= maxPriceLevel.Value);
            }

            // 5. Alkol Filtresi
            if (requiresAlcohol.HasValue && requiresAlcohol.Value)
            {
                query = query.Where(v => v.HasAlcohol);
            }

            // 6. Otopark / Vale Filtresi
            if (requiresParking.HasValue && requiresParking.Value)
            {
                query = query.Where(v => v.HasValetParking);
            }

            // 7. Açık Hava / Bahçe / Teras Filtresi
            if (requiresOutdoor.HasValue && requiresOutdoor.Value)
            {
                query = query.Where(v => v.HasOutdoorSeating);
            }

            // 8. Huni 1. Aşama (Stage 1): Kalite ve Güven Odaklı SQL Sıralaması (ORDER BY + TOP N)
            // Rastgele ilk 40'ı almak yerine, en yüksek puanlı ve en güvenilir aday havuzunu (150 mekan) çekiyoruz.
            var result = await query
                .OrderByDescending(v => v.GoogleRating)
                .ThenByDescending(v => v.ReviewCount)
                .Take(candidateLimit > 0 ? candidateLimit : 150)
                .ToListAsync(cancellationToken);

            // Eğer sıkı filtreler sonucu havuz çok küçük kalırsa (örn < 5), esnek arama yap
            if (result.Count < 5 && (requiresParking == true || requiresOutdoor == true))
            {
                var relaxedQuery = _context.Venues.AsNoTracking().AsQueryable();
                if (coveredDistricts != null && coveredDistricts.Any())
                {
                    var lowerDistricts = coveredDistricts.Select(d => d.ToLower()).ToList();
                    relaxedQuery = relaxedQuery.Where(v => lowerDistricts.Contains(v.District.ToLower()));
                }
                else if (!string.IsNullOrWhiteSpace(district) && !district.StartsWith("Tüm"))
                {
                    relaxedQuery = relaxedQuery.Where(v => v.District.ToLower() == district.ToLower());
                }

                if (requiresAlcohol.HasValue && requiresAlcohol.Value)
                {
                    relaxedQuery = relaxedQuery.Where(v => v.HasAlcohol);
                }

                var relaxedResults = await relaxedQuery
                    .OrderByDescending(v => v.GoogleRating)
                    .ThenByDescending(v => v.ReviewCount)
                    .Take(candidateLimit > 0 ? candidateLimit : 150)
                    .ToListAsync(cancellationToken);

                if (relaxedResults.Count > result.Count)
                {
                    result = relaxedResults;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VenueRepository] DB query failed: {ex.Message}");
            return new List<Venue>();
        }
    }

    public async Task<List<string>> GetAvailableCountriesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Venues
                .AsNoTracking()
                .Select(v => v.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);
        }
        catch
        {
            return new List<string> { "Türkiye" };
        }
    }

    public async Task<List<string>> GetAvailableCitiesAsync(string? country = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Venues.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(v => v.Country.ToLower() == country.ToLower() || v.CountryCode.ToLower() == country.ToLower());
            }

            return await query
                .Select(v => v.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(cancellationToken);
        }
        catch
        {
            return new List<string> { "İstanbul" };
        }
    }

    public async Task<List<Venue>> GetAllVenuesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Venues.AsNoTracking().Take(50).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VenueRepository] GetAllVenuesAsync DB query failed: {ex.Message}");
            return new List<Venue>();
        }
    }

    public async Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task AddRangeAsync(IEnumerable<Venue> venues, CancellationToken cancellationToken = default)
    {
        await _context.Venues.AddRangeAsync(venues, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Venues.CountAsync(cancellationToken);
        }
        catch
        {
            return 0;
        }
    }
}
