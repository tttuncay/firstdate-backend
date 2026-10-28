using Microsoft.EntityFrameworkCore;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;
using DateSpot.Core.Interfaces;
using DateSpot.Infrastructure.Data;

namespace DateSpot.Infrastructure.Repositories;

public class VenueRepository : IVenueRepository
{
    private readonly DateSpotDbContext _context;

    // İlk buluşma ve kutlama gibi özenli konseptlerde elenecek salaş/hızlı tüketim kelimeleri
    private static readonly string[] FastFoodKeywords =
    {
        "döner", "doner", "büfe", "bufe", "çiğköfte", "cigkofte", "pide", "lahmacun",
        "kokoreç", "kokorec", "paça", "işkembe", "tantuni", "kantin", "pilav",
        "simit", "kumpir", "tost", "esnaf lokantası", "tabldot"
    };

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
        int? noisePreference = null,
        string? groupSize = null,
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

            // 9. Huni 1. Aşama (Stage 1): Seçilen ilçelerdeki tüm aday kümesini çekip çok boyutlu puanlama motoruna sokma
            int fetchPoolSize = candidateLimit > 0 ? Math.Max(candidateLimit * 35, 1500) : 1500;
            var pool = await query
                .OrderByDescending(v => v.GoogleRating)
                .ThenByDescending(v => v.ReviewCount)
                .Take(fetchPoolSize)
                .ToListAsync(cancellationToken);

            if (!pool.Any())
            {
                return pool;
            }

            string combinedSearch = $"{venueType} {customPrompt}".Trim();
            int finalLimit = candidateLimit > 0 ? candidateLimit : 150;

            // Çok Boyutlu Puanlama (Buluşma Türü Ödül/Ceza + Mekan Tarzı + Saat Dilimi + Ses + Kişi Sayısı + Bayesian Kalite)
            var scoredList = pool
                .Select(v => new
                {
                    Venue = v,
                    RelevanceScore = CalculatePromptRelevance(
                        v,
                        combinedSearch,
                        occasion,
                        timeSlot,
                        noisePreference,
                        groupSize),
                    BayesianQuality = CalculateBayesianQualityScore(v)
                })
                .OrderByDescending(x => x.RelevanceScore + x.BayesianQuality)
                .ThenByDescending(x => x.BayesianQuality)
                .ToList();

            // Zincir / Mükerrer İsim Çeşitlendirme (Aynı isimli şubelerin ilk 40 listesini domine etmesini engeller)
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var diversified = new List<Venue>(finalLimit);
            var overflow = new List<Venue>();

            foreach (var item in scoredList)
            {
                string normalizedName = NormalizeBrandName(item.Venue.Name);
                if (seenNames.Add(normalizedName))
                {
                    diversified.Add(item.Venue);
                    if (diversified.Count >= finalLimit)
                        break;
                }
                else
                {
                    overflow.Add(item.Venue);
                }
            }

            if (diversified.Count < finalLimit && overflow.Any())
            {
                diversified.AddRange(overflow.Take(finalLimit - diversified.Count));
            }

            return diversified;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VenueRepository] DB query failed: {ex.Message}");
            return new List<Venue>();
        }
    }

    /// <summary>
    /// Google Puanı ve Yorum Sayısını birleştiren Bayesian Ağırlıklı Kalite Puanı (Yaklaşık 15 - 75 puan arası).
    /// 4.9 puanlı 105 yorumlu bir yerin, 4.7 puanlı 4.500 yorumlu ikonik bir mekanı haksız geçmesini dengeler.
    /// </summary>
    public static double CalculateBayesianQualityScore(Venue v)
    {
        const double m = 220.0; // Güven eşiği yorum sayısı
        const double c = 4.45;  // İstanbul elit mekan ortalama puanı
        double reviews = Math.Max(50, v.ReviewCount);
        double bayesianRating = ((reviews * v.GoogleRating) + (m * c)) / (reviews + m);
        double popularityBonus = Math.Min(18.0, Math.Log10(reviews) * 4.5);
        return Math.Max(0.0, (bayesianRating - 4.0) * 55.0) + popularityBonus;
    }

    private static string NormalizeBrandName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 2 ? name.Trim().ToLowerInvariant() : $"{parts[0]} {parts[1]}".ToLowerInvariant();
    }

    public static double CalculatePromptRelevance(
        Venue v,
        string prompt,
        string? occasion = null,
        string? timeSlot = null,
        int? noisePreference = null,
        string? groupSize = null)
    {
        var p = (prompt ?? string.Empty).ToLower();
        var occ = (occasion ?? string.Empty).ToLower();
        var slot = (timeSlot ?? string.Empty).ToLower();
        var grp = (groupSize ?? string.Empty).ToLower();
        double score = 0.0;

        var vNameLower = (v.Name ?? string.Empty).ToLower();
        var cuisinesStr = string.Join(" ", v.CuisineTypes ?? new()).ToLower();
        var signaturesStr = string.Join(" ", v.SignatureItems ?? new()).ToLower();
        var vibesStr = string.Join(" ", v.VibeTags ?? new()).ToLower();
        var viewsStr = string.Join(" ", v.ViewType ?? new()).ToLower();
        var occasionsStr = string.Join(" ", v.SuitableOccasions ?? new()).ToLower();
        var searchable = $"{vNameLower} {v.Neighborhood} {v.Address} {vibesStr} {cuisinesStr} {signaturesStr} {viewsStr} {occasionsStr} {v.BestTableTip}".ToLower();

        bool isFastFoodOrSalas = FastFoodKeywords.Any(kw => vNameLower.Contains(kw));
        bool isMeyhaneOrFish = cuisinesStr.Contains("meyhane") || cuisinesStr.Contains("deniz ürünleri") || vNameLower.Contains("meyhane") || vNameLower.Contains("balık") || vNameLower.Contains("balik");
        bool isMeatOrOcakbasi = cuisinesStr.Contains("ocakbaşı") || cuisinesStr.Contains("geleneksel türk") || vNameLower.Contains("ocakbaşı") || vNameLower.Contains("kebap") || vNameLower.Contains("mangal") || vNameLower.Contains("köfte");
        bool isBarOrPub = cuisinesStr.Contains("bar, pub") || vNameLower.Contains("pub") || vNameLower.Contains("bar") || vNameLower.Contains("kokteyl") || vNameLower.Contains("club") || vNameLower.Contains("lounge");
        bool isCafeOrSweet = cuisinesStr.Contains("3. nesil kahve") || cuisinesStr.Contains("tatlı") || vNameLower.Contains("kahve") || vNameLower.Contains("coffee") || vNameLower.Contains("kafe") || vNameLower.Contains("cafe") || vNameLower.Contains("roaster") || vNameLower.Contains("bakery") || vNameLower.Contains("kitap");
        bool isBistroOrItalian = cuisinesStr.Contains("italyan") || cuisinesStr.Contains("burger, pizza") || vNameLower.Contains("bistro") || vNameLower.Contains("pizza") || vNameLower.Contains("burger") || vNameLower.Contains("trattoria");

        // =========================================================================
        // 0. BULUŞMA TÜRÜ (OCCASION) ÖDÜL & CEZA MATRİSİ (BOOST & PENALTY MATRIX)
        // =========================================================================
        double promptMultiplier = 1.0;

        if (!string.IsNullOrWhiteSpace(occ))
        {
            // A) First Date / İlk Buluşma & Romantik
            if (occ.Contains("ilk buluşma") || occ.Contains("first date") || occ.Contains("romantik"))
            {
                if (occasionsStr.Contains("ilk buluşma") || occasionsStr.Contains("romantik"))
                    score += 140.0;

                if (v.FirstDateSuitabilityScore >= 8.4)
                    score += (v.FirstDateSuitabilityScore - 8.0) * 95.0;
                else if (v.FirstDateSuitabilityScore < 7.5)
                    score -= 220.0;

                if (v.NoiseLevel == NoiseLevel.ModerateMusic)
                    score += 75.0;
                else if (v.NoiseLevel == NoiseLevel.WhisperQuiet)
                    score += 45.0;
                else if (v.NoiseLevel == NoiseLevel.LivelyLoud && !p.Contains("bar") && !p.Contains("kokteyl"))
                    score -= 80.0;

                if (!string.IsNullOrEmpty(v.LightingStyle) && (v.LightingStyle.Contains("Loş", StringComparison.OrdinalIgnoreCase) || v.LightingStyle.Contains("Sıcak", StringComparison.OrdinalIgnoreCase)))
                    score += 65.0;

                if (viewsStr.Contains("boğaz") || viewsStr.Contains("deniz") || viewsStr.Contains("yeşil bahçe"))
                    score += 80.0;

                if (!string.IsNullOrEmpty(v.TableSpacing) && v.TableSpacing.Contains("Geniş", StringComparison.OrdinalIgnoreCase))
                    score += 65.0;

                // CEZA: İlk buluşmada salaş dönerci, büfe, çiğköfteci, pideci kesinlikle elenmeli
                if (isFastFoodOrSalas)
                    score -= 480.0;

                // Kullanıcı özel olarak et/kebap istemediyse dumanlı/ağır kebapçıları ilk buluşmada geriye it
                if (isMeatOrOcakbasi && v.PriceLevel < PriceLevel.Premium && !p.Contains("kebap") && !p.Contains("ocakbaşı"))
                    score -= 200.0;
            }
            // B) Arkadaş Buluşması & Sosyal
            else if (occ.Contains("arkadaş") || occ.Contains("sosyal"))
            {
                if (occasionsStr.Contains("arkadaş") || occasionsStr.Contains("sosyal"))
                    score += 160.0;

                if (v.NoiseLevel == NoiseLevel.LivelyLoud)
                    score += 95.0;
                else if (v.NoiseLevel == NoiseLevel.ModerateMusic)
                    score += 75.0;

                if (isBarOrPub || isMeyhaneOrFish || isMeatOrOcakbasi || isBistroOrItalian)
                    score += 130.0;
                else if (isCafeOrSweet)
                    score += 85.0;

                if (v.HasOutdoorSeating)
                    score += 45.0;

                // CEZA: Arkadaşlarla enerjik buluşmada fısıltı sessizliğindeki çalışma/kütüphane kafelerini geriye it
                if (v.NoiseLevel == NoiseLevel.WhisperQuiet && (vNameLower.Contains("kitap") || vNameLower.Contains("kütüphane")) && !p.Contains("kitap") && !p.Contains("sakin"))
                    score -= 180.0;
            }
            // C) Kahve & Çalışma / İş Buluşması
            else if (occ.Contains("çalışma") || occ.Contains("iş") || occ.Contains("kahve"))
            {
                if (occasionsStr.Contains("çalışma") || occasionsStr.Contains("iş") || occasionsStr.Contains("freelance"))
                    score += 180.0;

                if (v.HasWifiAndSockets)
                    score += 220.0;
                else
                    score -= 140.0;

                if (v.NoiseLevel == NoiseLevel.WhisperQuiet)
                    score += 180.0;
                else if (v.NoiseLevel == NoiseLevel.ModerateMusic)
                    score += 55.0;
                else if (v.NoiseLevel == NoiseLevel.LivelyLoud)
                    score -= 380.0;

                if (isCafeOrSweet)
                    score += 190.0;

                // KESİN CEZA: Kahve & Çalışma modunda meyhane, ocakbaşı, kebapçı, balıkçı, bar/gece kulübü asla önerilmemeli!
                if (isMeyhaneOrFish || isMeatOrOcakbasi || isBarOrPub || isFastFoodOrSalas)
                    score -= 550.0;
            }
            // D) Aile & Keyifli Sofra (Kahvaltı & Akşam Yemeği)
            else if (occ.Contains("aile") || occ.Contains("kahvaltı") || occ.Contains("sofra"))
            {
                if (occasionsStr.Contains("aile") || occasionsStr.Contains("kahvaltı"))
                    score += 170.0;

                if (cuisinesStr.Contains("kahvaltı") || isMeatOrOcakbasi || isMeyhaneOrFish || cuisinesStr.Contains("dünya mutfağı"))
                    score += 140.0;

                if (v.HasOutdoorSeating || viewsStr.Contains("yeşil bahçe") || viewsStr.Contains("boğaz"))
                    score += 85.0;

                if (v.HasValetParking)
                    score += 55.0;

                if (!string.IsNullOrEmpty(v.TableSpacing) && (v.TableSpacing.Contains("Ferah", StringComparison.OrdinalIgnoreCase) || v.TableSpacing.Contains("Geniş", StringComparison.OrdinalIgnoreCase)))
                    score += 70.0;

                if (v.NoiseLevel != NoiseLevel.LivelyLoud)
                    score += 65.0;
                else
                    score -= 220.0;

                // KESİN CEZA: Aile sofrasında bar, pub, gece kulübü, kokteyl bar elenmeli
                if (isBarOrPub)
                    score -= 480.0;
            }
            // E) Kutlama, Doğum Günü & Özel Gün
            else if (occ.Contains("kutlama") || occ.Contains("doğum günü") || occ.Contains("yıl dönümü"))
            {
                if (occasionsStr.Contains("kutlama") || occasionsStr.Contains("yıl dönümü") || occasionsStr.Contains("özel"))
                    score += 170.0;

                if (v.PriceLevel == PriceLevel.Premium)
                    score += 150.0;
                else if (v.PriceLevel == PriceLevel.Moderate)
                    score += 65.0;
                else if (v.PriceLevel == PriceLevel.Budget)
                    score -= 350.0;

                if (v.RequiresReservation)
                    score += 100.0;

                if (viewsStr.Contains("boğaz") || viewsStr.Contains("deniz"))
                    score += 130.0;

                if (vibesStr.Contains("büyüleyici") || vibesStr.Contains("fotojenik") || vibesStr.Contains("romantik"))
                    score += 95.0;

                // KESİN CEZA: Kutlama & Özel Günde salaş yerler ve sessiz çalışma/kitap kafeleri elenmeli
                if (isFastFoodOrSalas)
                    score -= 500.0;
                if (v.NoiseLevel == NoiseLevel.WhisperQuiet && isCafeOrSweet && !p.Contains("kahve") && !p.Contains("pasta"))
                    score -= 240.0;
            }
            // F) Özel Konsept & Serbest Keşif
            else if (occ.Contains("özel konsept") || occ.Contains("serbest"))
            {
                promptMultiplier = 1.35;
            }
        }

        // =========================================================================
        // 1. MEKAN TARZI (VENUE TYPE) & KULLANICI PROMPT NİYET BLOKLARI
        // =========================================================================
        double intentScore = 0.0;

        // 1.1 BALIK / BALIKÇI / DENİZ ÜRÜNLERİ / MEZE / SEAFOOD / MEYHANE NİYETİ
        bool isFishPrompt = p.Contains("balık") || p.Contains("balik") || p.Contains("balıkçı") || p.Contains("balikci") 
            || p.Contains("seafood") || p.Contains("deniz ürünleri") || p.Contains("deniz urunleri") || p.Contains("kalamar") 
            || p.Contains("karides") || p.Contains("levrek") || p.Contains("çipura") || p.Contains("cipura") 
            || p.Contains("ahtapot") || p.Contains("hamsi") || p.Contains("rakı balık") || p.Contains("raki balik") 
            || p.Contains("midye") || p.Contains("balık ekmek");

        if (isFishPrompt)
        {
            bool isFishVenue = isMeyhaneOrFish || searchable.Contains("fish") || searchable.Contains("seafood") || searchable.Contains("meze");
            if (isFishVenue)
            {
                intentScore += 340.0;
                if (vNameLower.Contains("balık") || vNameLower.Contains("balik") || vNameLower.Contains("fish") || vNameLower.Contains("meyhane"))
                    intentScore += 120.0;
            }
            else
            {
                intentScore -= 260.0;
            }
        }

        // 1.2 MANGAL / ET / OCAKBAŞI / KEBAP / STEAK / IZGARA / GELENEKSEL TÜRK NİYETİ
        bool isMeatPrompt = p.Contains("mangal") || p.Contains(" et ") || p.StartsWith("et ") || p.EndsWith(" et")
            || p.Contains("ocakbaşı") || p.Contains("ocakbasi") || p.Contains("kebap") || p.Contains("kebapçı")
            || p.Contains("kebapci") || p.Contains("ızgara") || p.Contains("izgara") || p.Contains("köfte")
            || p.Contains("kofte") || p.Contains("biftek") || p.Contains("kendin pişir") || p.Contains("etçi")
            || p.Contains("steak") || p.Contains("döner") || p.Contains("antrikot") || p.Contains("pirzola")
            || p.Contains("geleneksel türk");

        if (isMeatPrompt)
        {
            bool isMeatVenue = isMeatOrOcakbasi || searchable.Contains("steak") || searchable.Contains("et lokantası");
            if (isMeatVenue)
            {
                intentScore += 340.0;
                if (vNameLower.Contains("ocakbaşı") || vNameLower.Contains("kebap") || vNameLower.Contains("mangal") || vNameLower.Contains("steak") || vNameLower.Contains("köfte"))
                    intentScore += 120.0;
            }
            else
            {
                intentScore -= 260.0;
            }
        }

        // 1.3 DENİZ KENARI / SAHİL / BOĞAZ / MANZARA / TERAS / ROOFTOP NİYETİ
        bool isSeaPrompt = p.Contains("deniz") || p.Contains("sahil") || p.Contains("kordon") || p.Contains("boğaz") 
            || p.Contains("bogaz") || p.Contains("marina") || p.Contains("rıhtım") || p.Contains("rihtim") 
            || p.Contains("manzara") || p.Contains("dalga") || p.Contains("yalı") || p.Contains("yali") 
            || p.Contains("kıyı") || p.Contains("iskeleye") || p.Contains("rooftop") || p.Contains("panoramik");

        if (isSeaPrompt)
        {
            bool hasSeaView = viewsStr.Contains("deniz") || viewsStr.Contains("boğaz") || viewsStr.Contains("sahil") || vibesStr.Contains("boğaz");
            if (hasSeaView)
                intentScore += 260.0;
            else
                intentScore -= 140.0;

            if (!string.IsNullOrEmpty(v.Address) && (v.Address.Contains("Sahil", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Rıhtım", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Kordon", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Marina", StringComparison.OrdinalIgnoreCase) || v.Address.Contains("Yalı", StringComparison.OrdinalIgnoreCase)))
                intentScore += 95.0;

            if (vNameLower.Contains("sahil") || vNameLower.Contains("marina") || vNameLower.Contains("deniz") || vNameLower.Contains("yalı") || vNameLower.Contains("boğaz") || vNameLower.Contains("teras") || vNameLower.Contains("roof"))
                intentScore += 100.0;
        }

        // 1.4 TATLI / CHEESECAKE / SAN SEBASTIAN / KAHVE / BUTİK KAFE / KRUVASAN / BAKERY NİYETİ
        bool isDessertPrompt = p.Contains("tatlı") || p.Contains("tatli") || p.Contains("kahve") || p.Contains("cheesecake") 
            || p.Contains("san sebastian") || p.Contains("pasta") || p.Contains("kruvasan") || p.Contains("kafe") 
            || p.Contains("cafe") || p.Contains("çikolata") || p.Contains("cikolata") || p.Contains("roastery") 
            || p.Contains("fırın") || p.Contains("bakery") || p.Contains("waffle") || p.Contains("tiramisu")
            || p.Contains("butik kahve");

        if (isDessertPrompt)
        {
            if (isCafeOrSweet)
            {
                intentScore += 280.0;
                if (p.Contains("kruvasan") && (signaturesStr.Contains("kruvasan") || vNameLower.Contains("bakery") || vNameLower.Contains("fırın") || vNameLower.Contains("patisserie")))
                    intentScore += 120.0;
                if (p.Contains("san sebastian") && signaturesStr.Contains("san sebastian"))
                    intentScore += 110.0;
            }
            else if (isMeyhaneOrFish || isMeatOrOcakbasi || isBarOrPub)
            {
                intentScore -= 250.0;
            }
        }

        // 1.5 MEYHANE / RAKI / KOKTEYL / PUB / BİRA / ŞARAP / BAR / LOUNGE NİYETİ
        bool isDrinkPrompt = p.Contains("kokteyl") || p.Contains("pub") || p.Contains("bira") || p.Contains("bar") 
            || p.Contains("şarap") || p.Contains("sarap") || p.Contains("meyhane") || p.Contains("rakı") 
            || p.Contains("raki") || p.Contains("fasıl") || p.Contains("canlı müzik") || p.Contains("lounge");

        if (isDrinkPrompt)
        {
            if (v.HasAlcohol)
                intentScore += 120.0;
            else
                intentScore -= 300.0;

            if (p.Contains("meyhane") || p.Contains("rakı") || p.Contains("fasıl"))
            {
                if (isMeyhaneOrFish) intentScore += 260.0;
                else intentScore -= 180.0;
            }
            else if (isBarOrPub || searchable.Contains("kokteyl") || searchable.Contains("şarap"))
            {
                intentScore += 240.0;
            }
        }

        // 1.6 KAHVALTI & BRUNCH / SERPME KAHVALTI NİYETİ
        bool isBreakfastPrompt = p.Contains("kahvaltı") || p.Contains("kahvalti") || p.Contains("brunch") 
            || p.Contains("serpme") || p.Contains("menemen") || p.Contains("pancake") || p.Contains("omlet") || p.Contains("pişi");

        if (isBreakfastPrompt)
        {
            if (cuisinesStr.Contains("kahvaltı") || signaturesStr.Contains("kahvaltı") || vNameLower.Contains("kahvaltı") || vNameLower.Contains("brunch"))
                intentScore += 320.0;
            else if (isBarOrPub || isMeyhaneOrFish)
                intentScore -= 300.0;
        }

        // 1.7 SESSİZ / SAKİN / ÇALIŞMA / LAPTOP / PRİZ / KİTAP KAFE / İŞ TOPLANTISI NİYETİ
        bool isQuietPrompt = p.Contains("sessiz") || p.Contains("sakin") || p.Contains("çalışma") || p.Contains("calisma") 
            || p.Contains("laptop") || p.Contains("priz") || p.Contains("kitap") || p.Contains("huzur") || p.Contains("dingin")
            || p.Contains("toplantı") || p.Contains("kütüphane");

        if (isQuietPrompt)
        {
            if (v.NoiseLevel == NoiseLevel.WhisperQuiet) intentScore += 180.0;
            else if (v.NoiseLevel == NoiseLevel.LivelyLoud) intentScore -= 300.0;

            if (v.HasWifiAndSockets) intentScore += 140.0;
            if (vNameLower.Contains("kitap") && p.Contains("kitap")) intentScore += 180.0;
            if (isCafeOrSweet) intentScore += 120.0;
        }

        // 1.8 ROMANTİK RESTORAN / DÜNYA MUTFAĞI / İTALYAN / FINE DINING / YIL DÖNÜMÜ NİYETİ
        bool isDiningPrompt = p.Contains("restoran") || p.Contains("dünya mutfağı") || p.Contains("italyan")
            || p.Contains("makarna") || p.Contains("fine dining") || p.Contains("yıl dönümü")
            || p.Contains("akşam yemeği") || p.Contains("şık sofra") || p.Contains("nezih");

        if (isDiningPrompt)
        {
            if (cuisinesStr.Contains("italyan") || cuisinesStr.Contains("dünya mutfağı") || vNameLower.Contains("restoran") || vNameLower.Contains("restaurant") || vNameLower.Contains("bistro") || vNameLower.Contains("steak"))
                intentScore += 240.0;

            if (v.PriceLevel >= PriceLevel.Moderate)
                intentScore += 80.0;

            if ((p.Contains("fine dining") || p.Contains("yıl dönümü") || p.Contains("lüks")) && v.PriceLevel == PriceLevel.Premium)
                intentScore += 180.0;

            if (isFastFoodOrSalas)
                intentScore -= 400.0;
        }

        // 1.9 BURGER / PİZZA / SOSYAL BİSTRO / PAYLAŞIMLI LEZZETLER NİYETİ
        bool isCasualBistroPrompt = p.Contains("burger") || p.Contains("pizza") || p.Contains("bistro")
            || p.Contains("paylaşımlı") || p.Contains("atıştırmalık");

        if (isCasualBistroPrompt)
        {
            if (isBistroOrItalian || signaturesStr.Contains("pizza") || searchable.Contains("burger"))
                intentScore += 280.0;
        }

        // 1.10 DOĞUM GÜNÜ & GRUP KUTLAMA MEKANI NİYETİ
        bool isBirthdayPrompt = p.Contains("doğum günü") || p.Contains("kutlama") || p.Contains("grup");
        if (isBirthdayPrompt)
        {
            if (occasionsStr.Contains("kutlama") || isBarOrPub || isMeyhaneOrFish || isBistroOrItalian || viewsStr.Contains("boğaz"))
                intentScore += 200.0;
            if (v.PriceLevel >= PriceLevel.Moderate)
                intentScore += 70.0;
        }

        // 1.11 ROMANTİK / LOŞ IŞIK / ŞIK / BAŞ BAŞA NİYETİ
        bool isRomanticPrompt = p.Contains("romantik") || p.Contains("loş") || p.Contains("los") || p.Contains("mum") 
            || p.Contains("baş başa") || p.Contains("bas basa") || p.Contains("şık") || p.Contains("zarif");

        if (isRomanticPrompt)
        {
            if (vibesStr.Contains("romantik") || vibesStr.Contains("büyüleyici") || v.LightingStyle.Contains("Loş", StringComparison.OrdinalIgnoreCase))
                intentScore += 160.0;
            if (v.FirstDateSuitabilityScore >= 8.8)
                intentScore += 80.0;
        }

        // 1.12 AÇIK HAVA / BAHÇE / TERAS / KORU / PİKNİK / DOĞA NİYETİ
        bool isOutdoorPrompt = p.Contains("bahçe") || p.Contains("bahce") || p.Contains("açık hava") 
            || p.Contains("acik hava") || p.Contains("teras") || p.Contains("doğa") || p.Contains("doga")
            || p.Contains("yeşillik") || p.Contains("koru") || p.Contains("piknik") || p.Contains("seyir tepesi");

        if (isOutdoorPrompt)
        {
            if (v.HasOutdoorSeating) intentScore += 120.0;
            else intentScore -= 180.0;

            if (viewsStr.Contains("yeşil bahçe") || cuisinesStr.Contains("piknik") || vNameLower.Contains("koru") || vNameLower.Contains("park") || vNameLower.Contains("bahçe") || vNameLower.Contains("teras"))
                intentScore += 180.0;
        }

        // 1.13 EVCİL HAYVAN / KÖPEK DOSTU NİYETİ
        bool isPetPrompt = p.Contains("köpek") || p.Contains("kopek") || p.Contains("evcil hayvan") 
            || p.Contains("pet friendly") || p.Contains("hayvan dostu") || p.Contains("kedi");
        if (isPetPrompt)
        {
            if (v.IsPetFriendly) intentScore += 180.0;
            else intentScore -= 200.0;
            if (v.HasOutdoorSeating) intentScore += 80.0;
        }

        // 1.14 Kelime bazlı genel eşleşme
        var tokens = p.Split(new[] { ' ', ',', '.', '!', '?', '-', '/', '&', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            if (token.Length >= 3 && searchable.Contains(token))
            {
                intentScore += 22.0;
            }
        }

        score += intentScore * promptMultiplier;

        // =========================================================================
        // 2. SAAT DİLİMİ (TIME SLOT) UYUMU
        // =========================================================================
        if (!string.IsNullOrWhiteSpace(slot))
        {
            // Sabah / Kahvaltı (08:00 - 12:00)
            if (slot.Contains("sabah") || slot.Contains("kahvaltı") || slot.Contains("08:00"))
            {
                if (cuisinesStr.Contains("kahvaltı") || isCafeOrSweet || vNameLower.Contains("kahvaltı") || vNameLower.Contains("fırın") || vNameLower.Contains("bakery"))
                    score += 180.0;
                if (isBarOrPub || isMeyhaneOrFish || (isMeatOrOcakbasi && !cuisinesStr.Contains("kahvaltı")))
                    score -= 360.0;
            }
            // Öğlen / Gündüz (12:00 - 17:30)
            else if (slot.Contains("öğlen") || slot.Contains("gündüz") || slot.Contains("12:00"))
            {
                if (isCafeOrSweet || isBistroOrItalian || cuisinesStr.Contains("kahvaltı") || cuisinesStr.Contains("dünya mutfağı"))
                    score += 90.0;
                if (vNameLower.Contains("meyhane") || vNameLower.Contains("night") || vNameLower.Contains("club"))
                    score -= 240.0;
            }
            // Akşam (18:00 - 23:00)
            else if (slot.Contains("akşam") || slot.Contains("18:00"))
            {
                if (isMeyhaneOrFish || isMeatOrOcakbasi || isBistroOrItalian || isBarOrPub || v.PriceLevel >= PriceLevel.Moderate)
                    score += 90.0;
                if (vNameLower.Contains("börek") || vNameLower.Contains("simit") || vNameLower.Contains("kahvaltı evi"))
                    score -= 200.0;
            }
            // Gece / Geç Saat (23:00+)
            else if (slot.Contains("gece") || slot.Contains("23:00"))
            {
                if (v.HasAlcohol || isBarOrPub || isMeyhaneOrFish || v.NoiseLevel == NoiseLevel.LivelyLoud)
                    score += 200.0;
                if (v.NoiseLevel == NoiseLevel.WhisperQuiet || vNameLower.Contains("kahvaltı") || vNameLower.Contains("kitap") || vNameLower.Contains("fırın"))
                    score -= 260.0;
            }
        }

        // =========================================================================
        // 3. ORTAM SES TERCİHİ (NOISE PREFERENCE) & GRUP BÜYÜKLÜĞÜ (GROUP SIZE)
        // =========================================================================
        if (noisePreference.HasValue && noisePreference.Value >= 1 && noisePreference.Value <= 3)
        {
            int venueNoise = (int)v.NoiseLevel;
            int diff = Math.Abs(venueNoise - noisePreference.Value);
            if (diff == 0)
                score += 85.0;
            else if (diff == 2)
                score -= 130.0;
        }

        if (!string.IsNullOrWhiteSpace(grp))
        {
            if (grp.Contains("6+") || grp.Contains("kalabalık") || grp.Contains("3-5"))
            {
                if (v.HasOutdoorSeating || !string.IsNullOrEmpty(v.TableSpacing) && v.TableSpacing.Contains("Geniş", StringComparison.OrdinalIgnoreCase))
                    score += 55.0;
            }
            else if (grp.Contains("1-2") || grp.Contains("baş başa"))
            {
                if (v.SeatingTypes != null && v.SeatingTypes.Any(s => s.Contains("İkili", StringComparison.OrdinalIgnoreCase)))
                    score += 55.0;
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
