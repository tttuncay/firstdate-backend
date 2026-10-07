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
        bool? requiresPetFriendly = null,
        string? timeSlot = null,
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
                !country.Equals("Türkiye", StringComparison.OrdinalIgnoreCase) &&
                !country.Equals("Turkey", StringComparison.OrdinalIgnoreCase) &&
                !country.Equals("TR", StringComparison.OrdinalIgnoreCase) &&
                !country.Equals("Tümü", StringComparison.OrdinalIgnoreCase) && 
                !country.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !country.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var countryVariations = new List<string> { country, country.ToLower(), country.ToLowerInvariant() }.Distinct().ToList();
                query = query.Where(v => string.IsNullOrEmpty(v.Country) || countryVariations.Contains(v.Country) || countryVariations.Contains(v.CountryCode));
            }

            // 2. Şehir Filtresi (Opsiyonel & Güvenli)
            if (!string.IsNullOrWhiteSpace(city) && 
                !city.Equals("İstanbul", StringComparison.OrdinalIgnoreCase) &&
                !city.Equals("Istanbul", StringComparison.OrdinalIgnoreCase) &&
                !city.Equals("Tümü", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("Tüm Şehir", StringComparison.OrdinalIgnoreCase) && 
                !city.Equals("All Cities", StringComparison.OrdinalIgnoreCase))
            {
                var cityVariations = new List<string> { city, city.ToLower(), city.ToLowerInvariant() }.Distinct().ToList();
                query = query.Where(v => string.IsNullOrEmpty(v.City) || cityVariations.Contains(v.City));
            }

            // 3. İlçe / Bölge Filtresi (CoveredDistricts veya Tek İlçe)
            if (coveredDistricts != null && coveredDistricts.Any())
            {
                var allVariations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in coveredDistricts)
                {
                    if (string.IsNullOrWhiteSpace(d)) continue;
                    allVariations.Add(d);
                    allVariations.Add(d.ToLower());
                    allVariations.Add(d.ToLowerInvariant());
                    allVariations.Add(d.ToUpper());
                }
                var districtList = allVariations.ToList();
                query = query.Where(v => districtList.Contains(v.District));
            }
            else if (!string.IsNullOrWhiteSpace(district) && 
                !district.Equals("Tüm İstanbul", StringComparison.OrdinalIgnoreCase) &&
                !district.Equals("Tüm Şehir", StringComparison.OrdinalIgnoreCase) && 
                !district.Equals("Hepsi", StringComparison.OrdinalIgnoreCase) && 
                !district.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var districtVariations = new List<string> { district, district.ToLower(), district.ToLowerInvariant(), district.ToUpper() }.Distinct().ToList();
                query = query.Where(v => districtVariations.Contains(v.District));
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

            // 8. Evcil Hayvan / Pet Friendly Filtresi
            if (requiresPetFriendly.HasValue && requiresPetFriendly.Value)
            {
                query = query.Where(v => v.IsPetFriendly);
            }

            // 9. Huni 1. Aşama (Stage 1): Havuzdan daha geniş aday kümesi çekip prompt uyumuna göre sıralama
            int fetchPoolSize = candidateLimit > 0 ? Math.Max(candidateLimit * 10, 600) : 600;
            var pool = await query
                .OrderByDescending(v => v.GoogleRating)
                .ThenByDescending(v => v.ReviewCount)
                .Take(fetchPoolSize)
                .ToListAsync(cancellationToken);

            // Eğer prompt veya mekan tarzı seçilmişse, anlamsal ilgiye (Relevance) göre sırala
            string combinedSearch = $"{venueType} {customPrompt}".Trim();
            if (!string.IsNullOrWhiteSpace(combinedSearch) && pool.Any())
            {
                pool = pool
                    .Select(v => new { Venue = v, Score = CalculatePromptRelevance(v, combinedSearch) })
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

        var searchable = $"{v.Name} {v.Neighborhood} {v.Address} {string.Join(" ", v.VibeTags ?? new())} {string.Join(" ", v.CuisineTypes ?? new())} {string.Join(" ", v.SignatureItems ?? new())} {string.Join(" ", v.ViewType ?? new())} {string.Join(" ", v.SuitableOccasions ?? new())} {v.BestTableTip}".ToLower();

        // 1. BALIK / BALIKÇI / DENİZ ÜRÜNLERİ / MEZE / SEAFOOD NİYETİ
        bool isFishPrompt = p.Contains("balık") || p.Contains("balik") || p.Contains("balıkçı") || p.Contains("balikci") 
            || p.Contains("seafood") || p.Contains("deniz ürünleri") || p.Contains("deniz urunleri") || p.Contains("kalamar") 
            || p.Contains("karides") || p.Contains("levrek") || p.Contains("çipura") || p.Contains("cipura") 
            || p.Contains("ahtapot") || p.Contains("hamsi") || p.Contains("rakı balık") || p.Contains("raki balik") 
            || p.Contains("midye") || p.Contains("balık ekmek");

        if (isFishPrompt)
        {
            bool isFishVenue = searchable.Contains("balık") || searchable.Contains("balik") || searchable.Contains("fish") 
                || searchable.Contains("seafood") || searchable.Contains("deniz ürünleri") || searchable.Contains("meze") 
                || searchable.Contains("meyhane");

            if (isFishVenue)
            {
                score += 300.0;
                if (v.Name.Contains("Balık", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Balik", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Fish", StringComparison.OrdinalIgnoreCase))
                    score += 100.0;
            }
            else
            {
                // Balıkçı arayan birine tatlıcı veya kahveci önerilmemeli!
                bool isCafe = searchable.Contains("kahve") || searchable.Contains("coffee") || searchable.Contains("cafe") 
                    || searchable.Contains("kafe") || searchable.Contains("tatlı") || searchable.Contains("bakery") || searchable.Contains("pastane");
                if (isCafe)
                {
                    score -= 200.0;
                }
            }
        }

        // 2. MANGAL / ET / OCAKBAŞI / KEBAP / STEAK / IZGARA NİYETİ
        bool isMeatPrompt = p.Contains("mangal") || p.Contains("et") || p.Contains("ocakbaşı") || p.Contains("ocakbasi") 
            || p.Contains("kebap") || p.Contains("kebapçı") || p.Contains("kebapci") || p.Contains("ızgara") 
            || p.Contains("izgara") || p.Contains("köfte") || p.Contains("kofte") || p.Contains("biftek") 
            || p.Contains("kendin pişir") || p.Contains("etçi") || p.Contains("steak") || p.Contains("döner") 
            || p.Contains("antrikot") || p.Contains("pirzola");

        if (isMeatPrompt)
        {
            bool isMeatVenue = searchable.Contains("ocakbaşı") || searchable.Contains("kebap") || searchable.Contains("mangal") 
                || searchable.Contains("steak") || searchable.Contains("ızgara") || searchable.Contains("köfte") || searchable.Contains("et lokantası");

            if (isMeatVenue)
            {
                score += 300.0;
                if (v.Name.Contains("Ocakbaşı", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Kebap", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Mangal", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Steak", StringComparison.OrdinalIgnoreCase))
                    score += 100.0;
            }
            else
            {
                bool isCafe = searchable.Contains("kahve") || searchable.Contains("coffee") || searchable.Contains("cafe") 
                    || searchable.Contains("kafe") || searchable.Contains("tatlı") || searchable.Contains("bakery");
                if (isCafe)
                {
                    score -= 200.0;
                }
            }
        }

        // 3. DENİZ KENARI / SAHİL / BOĞAZ / DENİZ MANZARASI / KORDON / YALI / MARİNA NİYETİ
        bool isSeaPrompt = p.Contains("deniz") || p.Contains("sahil") || p.Contains("kordon") || p.Contains("boğaz") 
            || p.Contains("bogaz") || p.Contains("marina") || p.Contains("rıhtım") || p.Contains("rihtim") 
            || p.Contains("manzara") || p.Contains("dalga") || p.Contains("yalı") || p.Contains("yali") 
            || p.Contains("kıyı") || p.Contains("iskeleye") || p.Contains("kordon");

        if (isSeaPrompt)
        {
            if (v.ViewType != null && v.ViewType.Any(vt => vt.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Boğaz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Sahil", StringComparison.OrdinalIgnoreCase)))
                score += 120.0;

            if (v.VibeTags != null && v.VibeTags.Any(vt => vt.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Boğaz", StringComparison.OrdinalIgnoreCase) || vt.Contains("Manzara", StringComparison.OrdinalIgnoreCase)))
                score += 100.0;

            if (!string.IsNullOrEmpty(v.Address) && (v.Address.Contains("Sahil", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Rıhtım", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Kordon", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Marina", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Yalı", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Kordon", StringComparison.OrdinalIgnoreCase)))
                score += 80.0;

            if (v.Name.Contains("Sahil", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Marina", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Deniz", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Kıyı", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Yalı", StringComparison.OrdinalIgnoreCase) || v.Name.Contains("Boğaz", StringComparison.OrdinalIgnoreCase))
                score += 80.0;
        }

        // 4. TATLI / CHEESECAKE / SAN SEBASTIAN / KAHVE / BUTİK KAFE / KRUVASAN NİYETİ
        bool isDessertPrompt = p.Contains("tatlı") || p.Contains("tatli") || p.Contains("kahve") || p.Contains("cheesecake") 
            || p.Contains("san sebastian") || p.Contains("pasta") || p.Contains("kruvasan") || p.Contains("kafe") 
            || p.Contains("cafe") || p.Contains("çikolata") || p.Contains("cikolata") || p.Contains("roastery") 
            || p.Contains("fırın") || p.Contains("bakery") || p.Contains("waffle") || p.Contains("tiramisu");

        if (isDessertPrompt)
        {
            bool isDessertVenue = searchable.Contains("tatlı") || searchable.Contains("kahve") || searchable.Contains("cafe") 
                || searchable.Contains("kafe") || searchable.Contains("bakery") || searchable.Contains("pasta") || searchable.Contains("roaster");

            if (isDessertVenue)
            {
                score += 250.0;
            }
        }

        // 5. MEYHANE / RAKI / KOKTEYL / PUB / BİRA / ŞARAP / BAR NİYETİ
        bool isDrinkPrompt = p.Contains("kokteyl") || p.Contains("pub") || p.Contains("bira") || p.Contains("bar") 
            || p.Contains("şarap") || p.Contains("sarap") || p.Contains("meyhane") || p.Contains("rakı") 
            || p.Contains("raki") || p.Contains("fasıl") || p.Contains("canlı müzik");

        if (isDrinkPrompt)
        {
            if (v.HasAlcohol) score += 60.0;
            if (searchable.Contains("meyhane") || searchable.Contains("kokteyl") || searchable.Contains("pub") || searchable.Contains("bar") || searchable.Contains("şarap"))
                score += 200.0;
        }

        // 6. KAHVALTI & BRUNCH / SERPME KAHVALTI NİYETİ
        bool isBreakfastPrompt = p.Contains("kahvaltı") || p.Contains("kahvalti") || p.Contains("brunch") 
            || p.Contains("serpme") || p.Contains("menemen") || p.Contains("pancake") || p.Contains("omlet");

        if (isBreakfastPrompt)
        {
            if (searchable.Contains("kahvaltı") || searchable.Contains("brunch") || searchable.Contains("serpme"))
                score += 250.0;
        }

        // 7. SESSİZ / SAKİN / ÇALIŞMA / LAPTOP / PRİZ / HUZUR NİYETİ
        bool isQuietPrompt = p.Contains("sessiz") || p.Contains("sakin") || p.Contains("çalışma") || p.Contains("calisma") 
            || p.Contains("laptop") || p.Contains("priz") || p.Contains("kitap") || p.Contains("huzur") || p.Contains("dingin");

        if (isQuietPrompt)
        {
            if (v.NoiseLevel == NoiseLevel.WhisperQuiet) score += 120.0;
            if (v.HasWifiAndSockets) score += 80.0;
            if (searchable.Contains("sessiz") || searchable.Contains("sakin") || searchable.Contains("huzur") || searchable.Contains("çalışma"))
                score += 100.0;
        }

        // 8. ROMANTİK / LOŞ IŞIK / ŞIK / İLK BULUŞMA NİYETİ
        bool isRomanticPrompt = p.Contains("romantik") || p.Contains("loş") || p.Contains("los") || p.Contains("mum") 
            || p.Contains("baş başa") || p.Contains("bas basa") || p.Contains("şık") || p.Contains("zarif");

        if (isRomanticPrompt)
        {
            if (searchable.Contains("romantik") || searchable.Contains("şık") || searchable.Contains("loş"))
                score += 150.0;
            if (v.FirstDateSuitabilityScore >= 9.0) score += 60.0;
        }

        // 9. AÇIK HAVA / BAHÇE / TERAS NİYETİ
        bool isOutdoorPrompt = p.Contains("bahçe") || p.Contains("bahce") || p.Contains("açık hava") 
            || p.Contains("acik hava") || p.Contains("teras") || p.Contains("doğa") || p.Contains("doga") || p.Contains("yeşillik");

        if (isOutdoorPrompt)
        {
            if (v.HasOutdoorSeating) score += 80.0;
            if (searchable.Contains("bahçe") || searchable.Contains("teras") || searchable.Contains("doğa"))
                score += 70.0;
        }

        // 10. EVCİL HAYVAN / KÖPEK DOSTU NİYETİ (Prompt üzerinden akıllı ve esnek eşleşme)
        bool isPetPrompt = p.Contains("köpek") || p.Contains("kopek") || p.Contains("evcil hayvan") 
            || p.Contains("pet friendly") || p.Contains("hayvan dostu") || p.Contains("kedi");
        if (isPetPrompt)
        {
            if (v.IsPetFriendly) score += 120.0;
            if (v.HasOutdoorSeating) score += 80.0;
            if (searchable.Contains("bahçe") || searchable.Contains("teras") || searchable.Contains("park")) score += 60.0;
        }

        // 11. Kelime bazlı genel eşleşme
        var tokens = p.Split(new[] { ' ', ',', '.', '!', '?', '-', '/', '&' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if (token.Length >= 3 && searchable.Contains(token))
            {
                score += 25.0;
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
