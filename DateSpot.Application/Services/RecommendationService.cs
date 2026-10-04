using DateSpot.Application.DTOs;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;
using DateSpot.Core.Interfaces;

namespace DateSpot.Application.Services;

public interface IRecommendationService
{
    Task<RecommendationResponseDto> GetTopRecommendationsAsync(
        RecommendationRequestDto request,
        CancellationToken cancellationToken = default);
}

public class RecommendationService : IRecommendationService
{
    private readonly IVenueRepository _venueRepository;
    private readonly IGeminiAdvisorService _geminiAdvisorService;

    public RecommendationService(
        IVenueRepository venueRepository,
        IGeminiAdvisorService geminiAdvisorService)
    {
        _venueRepository = venueRepository;
        _geminiAdvisorService = geminiAdvisorService;
    }

    public async Task<RecommendationResponseDto> GetTopRecommendationsAsync(
        RecommendationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Veritabanından PostGIS / Çoklu İlçe Filtresi bazlı aday havuzunu çek (15 - 30 mekan)
        var candidates = await _venueRepository.GetVenuesByFiltersAsync(
            district: request.District,
            coveredDistricts: request.CoveredDistricts,
            userLat: request.Latitude,
            userLng: request.Longitude,
            radiusInKm: request.RadiusInKm,
            maxPriceLevel: request.MaxPriceLevel,
            requiresAlcohol: request.AlcoholRequired,
            requiresParking: request.ParkingRequired,
            cancellationToken: cancellationToken);

        if (candidates == null || !candidates.Any())
        {
            var allVenues = await _venueRepository.GetAllVenuesAsync(cancellationToken);
            if (allVenues.Any())
            {
                candidates = allVenues;
            }
            else
            {
                return new RecommendationResponseDto
                {
                    Success = false,
                    TotalCandidatesAnalyzed = 0,
                    RecommendedVenues = new List<VenueRecommendationDto>(),
                    Message = "Maalesef veritabanımızda uygun mekan eklenmemiş."
                };
            }
        }

        // 2. Ağırlıklı Puanlama Algoritması
        var scoredVenues = candidates.Select(venue =>
        {
            double score = CalculateFirstDateScore(venue, request);
            double distance = 0.0;

            if (request.Latitude.HasValue && request.Longitude.HasValue && venue.Location != null)
            {
                distance = CalculateDistanceKm(request.Latitude.Value, request.Longitude.Value, venue.Latitude, venue.Longitude);
            }

            return new
            {
                Venue = venue,
                Score = score,
                Distance = distance
            };
        })
        .OrderByDescending(x => x.Score)
        .Take(3)
        .ToList();

        var topVenues = scoredVenues.Select(x => x.Venue).ToList();

        // 3. Gemini AI ile mekana ve etkinlik amacına özel analizler üret
        var aiAdvices = await _geminiAdvisorService.GenerateDateAdvicesAsync(
            topVenues,
            request.Concept,
            request.District,
            request.Occasion,
            cancellationToken);

        // 4. Sonuç DTO'sunu derle
        var resultList = scoredVenues.Select(item =>
        {
            var v = item.Venue;
            var advice = aiAdvices.ContainsKey(v.Id) 
                ? aiAdvices[v.Id] 
                : new DateSpotAdviceResult
                {
                    WhyThisSpot = "İlk buluşmanın gerginliğini azaltan, samimi ve kaliteli bir atmosfere sahiptir.",
                    IcebreakerTopic = "Mekanın ambiyansı, menüdeki özel tatlar ve geçmiş seyahat anıları üzerine sohbet başlatabilirsiniz.",
                    TableTactics = "Karşılıklı yerine L şeklinde oturabileceğiniz bir köşe masa tercih edin.",
                    IdealOrderRecommendation = "İmza içecekleri ve paylaşımlı başlangıç tabağı."
                };

            // 0 - 100 arası normalize edilmiş eşleşme yüzdesi (Genelde %85 - %98 arası)
            double matchPercent = Math.Clamp(Math.Round(item.Score, 1), 75.0, 99.0);

            return new VenueRecommendationDto
            {
                Id = v.Id,
                Name = v.Name,
                District = v.District,
                Neighborhood = v.Neighborhood,
                Address = v.Address,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                DistanceInKm = Math.Round(item.Distance, 1),
                PriceLevel = v.PriceLevel,
                NoiseLevel = v.NoiseLevel,
                VibeTags = v.VibeTags,
                SeatingArrangement = v.SeatingArrangement,
                HasAlcohol = v.HasAlcohol,
                HasValetParking = v.HasValetParking,
                RequiresReservation = v.RequiresReservation,
                HasOutdoorSeating = v.HasOutdoorSeating,
                GoogleRating = v.GoogleRating,
                MatchScore = matchPercent,
                HeroImageUrl = v.HeroImageUrl,
                GalleryImages = v.GalleryImages,
                GoogleMapsUrl = v.GoogleMapsUrl,
                InstagramHandle = v.InstagramHandle,
                Advice = new DateAdviceDto
                {
                    WhyThisSpot = advice.WhyThisSpot,
                    IcebreakerTopic = advice.IcebreakerTopic,
                    TableTactics = advice.TableTactics,
                    IdealOrderRecommendation = advice.IdealOrderRecommendation
                }
            };
        }).ToList();

        return new RecommendationResponseDto
        {
            Success = true,
            TotalCandidatesAnalyzed = candidates.Count,
            RecommendedVenues = resultList,
            Message = $"{resultList.Count} mükemmel first date mekanı yapay zeka tarafından senin için hazırlandı."
        };
    }

    private static double CalculateFirstDateScore(Venue venue, RecommendationRequestDto req)
    {
        double score = 50.0; // Baz Puan

        // 1. Konsept & Mekan Tipi Uyumluluğu (Max: +30 Puan)
        if (!string.IsNullOrWhiteSpace(req.VenueType))
        {
            var vt = req.VenueType.ToLower();
            var allVibeStr = string.Join(" ", venue.VibeTags).ToLower() + " " + venue.Name.ToLower();

            if (vt.Contains("butik") || vt.Contains("3. nesil"))
            {
                if (allVibeStr.Contains("kahve") || allVibeStr.Contains("tatlı") || allVibeStr.Contains("butik") || venue.CompatibleConcepts.Contains(DateConcept.CoffeeAndWalk))
                    score += 25.0;
            }
            else if (vt.Contains("klasik") || vt.Contains("zincir"))
            {
                if (allVibeStr.Contains("kahve") || venue.CompatibleConcepts.Contains(DateConcept.CoffeeAndWalk) || venue.PriceLevel <= PriceLevel.Moderate)
                    score += 20.0;
            }
            else if (vt.Contains("sahil") || vt.Contains("deniz"))
            {
                if (venue.HasOutdoorSeating || allVibeStr.Contains("deniz") || allVibeStr.Contains("boğaz") || allVibeStr.Contains("manzara") || allVibeStr.Contains("sahil") || allVibeStr.Contains("teras"))
                    score += 25.0;
            }
            else if (vt.Contains("kokteyl") || vt.Contains("bar") || vt.Contains("pub"))
            {
                if (venue.HasAlcohol && (allVibeStr.Contains("kokteyl") || allVibeStr.Contains("bar") || allVibeStr.Contains("imza") || venue.CompatibleConcepts.Contains(DateConcept.CocktailAndVibe)))
                    score += 25.0;
            }
            else if (vt.Contains("şarap") || vt.Contains("mahzen"))
            {
                if (venue.HasAlcohol && (allVibeStr.Contains("şarap") || allVibeStr.Contains("mahzen") || allVibeStr.Contains("romantik") || venue.CompatibleConcepts.Contains(DateConcept.RomanticAndChic)))
                    score += 25.0;
            }
            else if (vt.Contains("şık") || vt.Contains("akşam yemeği") || vt.Contains("restoran"))
            {
                if (allVibeStr.Contains("yemek") || allVibeStr.Contains("restoran") || allVibeStr.Contains("fine dining") || allVibeStr.Contains("şık") || venue.PriceLevel >= PriceLevel.Moderate)
                    score += 25.0;
            }
        }
        else if (venue.CompatibleConcepts.Contains(req.Concept))
        {
            score += 25.0;
        }
        else if (venue.CompatibleConcepts.Any())
        {
            score += 12.0;
        }

        // 2. Vibe Archetype / Atmosfer Etiket Eşleşmesi (Max: +20 Puan)
        if (!string.IsNullOrWhiteSpace(req.VibeArchetype))
        {
            var lowerVibe = req.VibeArchetype.ToLower();
            int matchedTags = venue.VibeTags.Count(tag => 
                lowerVibe.Contains(tag.ToLower()) || 
                tag.ToLower().Split(' ').Any(w => w.Length > 3 && lowerVibe.Contains(w)));

            if (matchedTags > 0)
            {
                score += Math.Min(matchedTags * 8.0, 20.0);
            }
        }

        // 3. Oturma Düzeni & Mahremiyet Eşleşmesi (Max: +10 Puan)
        if (!string.IsNullOrWhiteSpace(req.SeatingPrivacy) && !string.IsNullOrWhiteSpace(venue.SeatingArrangement))
        {
            var lowerSeatingReq = req.SeatingPrivacy.ToLower();
            var lowerSeatingVenue = venue.SeatingArrangement.ToLower();

            if ((lowerSeatingReq.Contains("izole") && lowerSeatingVenue.Contains("mahzen")) ||
                (lowerSeatingReq.Contains("bar") && lowerSeatingVenue.Contains("bar")) ||
                (lowerSeatingReq.Contains("bahçe") && (venue.HasOutdoorSeating || lowerSeatingVenue.Contains("bahçe"))) ||
                (lowerSeatingReq.Contains("berjer") && lowerSeatingVenue.Contains("koltuk")))
            {
                score += 10.0;
            }
        }

        // 4. Uzman First Date Uygunluk Puanı (1.0 - 10.0 -> Max: +15 Puan)
        score += (venue.FirstDateSuitabilityScore * 1.5);

        // 5. Gürültü Seviyesi & Akustik Tercih
        if (req.NoisePreference.HasValue)
        {
            switch (req.NoisePreference.Value)
            {
                case 1: // Sakin
                    if (venue.NoiseLevel == NoiseLevel.WhisperQuiet) score += 12.0;
                    else if (venue.NoiseLevel == NoiseLevel.ModerateMusic) score += 6.0;
                    else score -= 15.0;
                    break;
                case 2: // Dengeli (İdeal)
                    if (venue.NoiseLevel == NoiseLevel.ModerateMusic) score += 12.0;
                    else if (venue.NoiseLevel == NoiseLevel.WhisperQuiet) score += 8.0;
                    else score += 4.0;
                    break;
                case 3: // Canlı & Enerjik
                    if (venue.NoiseLevel == NoiseLevel.LivelyLoud) score += 12.0;
                    else if (venue.NoiseLevel == NoiseLevel.ModerateMusic) score += 6.0;
                    break;
            }
        }
        else
        {
            switch (req.Concept)
            {
                case DateConcept.QuietAndIntimate:
                case DateConcept.RomanticAndChic:
                    if (venue.NoiseLevel == NoiseLevel.WhisperQuiet || venue.NoiseLevel == NoiseLevel.ModerateMusic)
                        score += 10.0;
                    else
                        score -= 15.0;
                    break;

                case DateConcept.CocktailAndVibe:
                case DateConcept.FunAndCasual:
                    if (venue.NoiseLevel == NoiseLevel.ModerateMusic || venue.NoiseLevel == NoiseLevel.LivelyLoud)
                        score += 10.0;
                    break;
            }
        }

        // 6. Google Puanı (Max: +5 Puan)
        score += (venue.GoogleRating - 4.0) * 5.0;

        // 7. Bütçe Eşleşmesi
        if (req.MaxPriceLevel.HasValue)
        {
            if (venue.PriceLevel <= req.MaxPriceLevel.Value)
                score += 5.0;
            else
                score -= 10.0;
        }

        return score;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var rlat1 = Math.PI * lat1 / 180;
        var rlat2 = Math.PI * lat2 / 180;
        var theta = lon1 - lon2;
        var rtheta = Math.PI * theta / 180;
        var dist = Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) * Math.Cos(rlat2) * Math.Cos(rtheta);
        dist = Math.Acos(Math.Min(1.0, dist));
        dist = dist * 180 / Math.PI;
        dist = dist * 60 * 1.1515 * 1.609344;
        return dist;
    }
}
