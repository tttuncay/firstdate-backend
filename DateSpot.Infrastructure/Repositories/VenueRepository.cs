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
        string? customPrompt = null,
        int candidateLimit = 150,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Venues.AsNoTracking().AsQueryable();

            // 1. Ülke Filtresi (Opsiyonel & Güvenli)
            if (!string.IsNullOrWhiteSpace(country) && 
                !country.Equals("Tümü", StringComparison.OrdinalIgnoreCase) && 
                !country.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !country.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var lowerCountry = country.ToLower();
                query = query.Where(v => string.IsNullOrEmpty(v.Country) || v.Country.ToLower() == lowerCountry || v.CountryCode.ToLower() == lowerCountry);
            }

            // 2. Şehir Filtresi (Opsiyonel & Güvenli)
            if (!string.IsNullOrWhiteSpace(city) && 
                !city.Equals("Tümü", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("Tüm Şehir", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("All Cities", StringComparison.OrdinalIgnoreCase))
            {
                var lowerCity = city.ToLower();
                query = query.Where(v => string.IsNullOrEmpty(v.City) || v.City.ToLower() == lowerCity);
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
                var lowerDistrict = district.ToLower();
                query = query.Where(v => v.District.ToLower() == lowerDistrict);
            }

            // 4. Bütçe Filtresi
            if (maxPriceLevel.HasValue && maxPriceLevel.Value > 0)
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

            // 8. Huni 1. Aşama (Stage 1): Havuzdan daha geniş aday kümesi çekip prompt uyumuna göre sıralama
            int fetchPoolSize = candidateLimit > 0 ? Math.Max(candidateLimit * 3, 200) : 300;
            var pool = await query
                .OrderByDescending(v => v.GoogleRating)
                .ThenByDescending(v => v.ReviewCount)
                .Take(fetchPoolSize)
                .ToListAsync(cancellationToken);

            // Eğer prompt girilmişse, prompt anlamsal ilgisine (Relevance) göre sırala
            if (!string.IsNullOrWhiteSpace(customPrompt) && pool.Any())
            {
                pool = pool
                    .Select(v => new { Venue = v, Score = CalculatePromptRelevance(v, customPrompt) })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Venue.GoogleRating)
                    .ThenByDescending(x => x.Venue.ReviewCount)
                    .Select(x => x.Venue)
                    .Take(candidateLimit > 0 ? candidateLimit : 150)
                    .ToList();
            }
            else
            {
                pool = pool.Take(candidateLimit > 0 ? candidateLimit : 150).ToList();
            }

            return pool;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VenueRepository] DB query failed: {ex.Message}");
            return new List<Venue>();
        }
    }

    public static double CalculatePromptRelevance(Venue v, string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return 0.0;
        
        var p = prompt.ToLower();
        double score = 0.0;

        // 1. Deniz / Sahil / Manzara / Boğaz Niyeti
        if (p.Contains("deniz") || p.Contains("sahil") || p.Contains("kordon") || p.Contains("boğaz") || p.Contains("bogaz") || p.Contains("marina") || p.Contains("rıhtım") || p.Contains("rihtim") || p.Contains("manzara") || p.Contains("dalga") || p.Contains("yalı") || p.Contains("yali") || p.Contains("kıyı") || p.Contains("iskeleye"))
        {
            if (v.ViewType != null && v.ViewType.Any(vt => vt.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Boğaz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Sahil", StringComparison.OrdinalIgnoreCase)))
                score += 100.0;

            if (v.VibeTags != null && v.VibeTags.Any(vt => vt.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Boğaz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Manzara", StringComparison.OrdinalIgnoreCase)))
                score += 80.0;

            if (!string.IsNullOrEmpty(v.Neighborhood) && (v.Neighborhood.Contains("Sahil", StringComparison.OrdinalIgnoreCase) || v.Neighborhood.Contains("Moda", StringComparison.OrdinalIgnoreCase) || v.Neighborhood.Contains("Bebek", StringComparison.OrdinalIgnoreCase) || v.Neighborhood.Contains("Kuzguncuk", StringComparison.OrdinalIgnoreCase) || v.Neighborhood.Contains("Kordon", StringComparison.OrdinalIgnoreCase) || v.Neighborhood.Contains("Marina", StringComparison.OrdinalIgnoreCase)))
                score += 60.0;

            if (!string.IsNullOrEmpty(v.Address) && (v.Address.Contains("Sahil", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Rıhtım", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Kordon", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Marina", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Yalı", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Deniz", StringComparison.OrdinalIgnoreCase)))
                score += 70.0;

            if (v.Name.Contains("Sahil", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Marina", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Kıyı", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Yalı", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Boğaz", StringComparison.OrdinalIgnoreCase))
                score += 75.0;
        }

        // 2. Mangal / Et / Ocakbaşı / Kebap Niyeti
        if (p.Contains("mangal") || p.Contains("et") || p.Contains("ocakbaşı") || p.Contains("ocakbasi") || p.Contains("kebap") || p.Contains("ızgara") || p.Contains("izgara") || p.Contains("köfte") || p.Contains("kofte") || p.Contains("biftek") || p.Contains("kendin pişir") || p.Contains("etçi"))
        {
            if (v.CuisineTypes != null && v.CuisineTypes.Any(c => c.Contains("Ocakbaşı", StringComparison.OrdinalIgnoreCase) || c.Contains("Kebap", StringComparison.OrdinalIgnoreCase) || c.Contains("Et", StringComparison.OrdinalIgnoreCase) || c.Contains("Mangal", StringComparison.OrdinalIgnoreCase)))
                score += 100.0;

            if (v.SignatureItems != null && v.SignatureItems.Any(s => s.Contains("Kebap", StringComparison.OrdinalIgnoreCase) || s.Contains("Et", StringComparison.OrdinalIgnoreCase) || s.Contains("Izgara", StringComparison.OrdinalIgnoreCase) || s.Contains("Köfte", StringComparison.OrdinalIgnoreCase)))
                score += 70.0;

            if (v.Name.Contains("Ocakbaşı", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Kebap", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Mangal", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Et", StringComparison.OrdinalIgnoreCase))
                score += 80.0;
        }

        // 3. Tatlı / Kahve / Butik Kafe Niyeti
        if (p.Contains("tatlı") || p.Contains("tatli") || p.Contains("kahve") || p.Contains("cheesecake") || p.Contains("pasta") || p.Contains("kruvasan") || p.Contains("kafe") || p.Contains("cafe") || p.Contains("çikolata") || p.Contains("roastery"))
        {
            if (v.CuisineTypes != null && v.CuisineTypes.Any(c => c.Contains("Tatlı", StringComparison.OrdinalIgnoreCase) || c.Contains("Kahve", StringComparison.OrdinalIgnoreCase) || c.Contains("Pasta", StringComparison.OrdinalIgnoreCase)))
                score += 80.0;

            if (v.SignatureItems != null && v.SignatureItems.Any(s => s.Contains("Cheesecake", StringComparison.OrdinalIgnoreCase) || s.Contains("Tatlı", StringComparison.OrdinalIgnoreCase) || s.Contains("Kahve", StringComparison.OrdinalIgnoreCase) || s.Contains("Kek", StringComparison.OrdinalIgnoreCase) || s.Contains("Pasta", StringComparison.OrdinalIgnoreCase)))
                score += 60.0;

            if (v.Name.Contains("Coffee", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Cafe", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Kafe", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Roaster", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Fırın", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Bakery", StringComparison.OrdinalIgnoreCase))
                score += 60.0;
        }

        // 4. Kokteyl / Pub / Bira / Şarap / Meyhane Niyeti
        if (p.Contains("kokteyl") || p.Contains("pub") || p.Contains("bira") || p.Contains("bar") || p.Contains("şarap") || p.Contains("sarap") || p.Contains("meyhane") || p.Contains("rakı") || p.Contains("raki"))
        {
            if (v.HasAlcohol) score += 50.0;
            if (v.CuisineTypes != null && v.CuisineTypes.Any(c => c.Contains("Kokteyl", StringComparison.OrdinalIgnoreCase) || c.Contains("Pub", StringComparison.OrdinalIgnoreCase) || c.Contains("Bar", StringComparison.OrdinalIgnoreCase) || c.Contains("Meyhane", StringComparison.OrdinalIgnoreCase)))
                score += 80.0;

            if (v.Name.Contains("Pub", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Bar", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Meyhane", StringComparison.OrdinalIgnoreCase))
                score += 60.0;
        }

        // 5. Sessiz / Sakin / Çalışma / Laptop Niyeti
        if (p.Contains("sessiz") || p.Contains("sakin") || p.Contains("çalışma") || p.Contains("calisma") || p.Contains("laptop") || p.Contains("priz") || p.Contains("kitap") || p.Contains("huzur"))
        {
            if (v.NoiseLevel == NoiseLevel.WhisperQuiet) score += 70.0;
            if (v.HasWifiAndSockets) score += 50.0;
            if (v.VibeTags != null && v.VibeTags.Any(t => t.Contains("Sessiz", StringComparison.OrdinalIgnoreCase) || t.Contains("Sakin", StringComparison.OrdinalIgnoreCase) || t.Contains("Huzur", StringComparison.OrdinalIgnoreCase)))
                score += 60.0;
        }

        // 6. Açık Hava / Bahçe / Teras Niyeti
        if (p.Contains("bahçe") || p.Contains("bahce") || p.Contains("açık hava") || p.Contains("acik hava") || p.Contains("teras") || p.Contains("doğa") || p.Contains("doga") || p.Contains("yeşillik") || p.Contains("piknik"))
        {
            if (v.HasOutdoorSeating) score += 70.0;
            if (v.VibeTags != null && v.VibeTags.Any(t => t.Contains("Bahçe", StringComparison.OrdinalIgnoreCase) || t.Contains("Teras", StringComparison.OrdinalIgnoreCase) || t.Contains("Doğa", StringComparison.OrdinalIgnoreCase)))
                score += 60.0;
            if (v.ViewType != null && v.ViewType.Any(t => t.Contains("Bahçe", StringComparison.OrdinalIgnoreCase) || t.Contains("Doğa", StringComparison.OrdinalIgnoreCase)))
                score += 50.0;
        }

        // 7. Kelime bazlı genel eşleşme
        var tokens = p.Split(new[] { ' ', ',', '.', '!', '?', '-', '/', '&' }, StringSplitOptions.RemoveEmptyEntries);
        var searchable = $"{v.Name} {v.Neighborhood} {v.Address} {string.Join(" ", v.VibeTags ?? new())} {string.Join(" ", v.CuisineTypes ?? new())} {string.Join(" ", v.SignatureItems ?? new())} {string.Join(" ", v.ViewType ?? new())} {v.BestTableTip}".ToLower();

        foreach (var token in tokens)
        {
            if (token.Length >= 3 && searchable.Contains(token))
            {
                score += 20.0;
            }
        }

        return score;
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
