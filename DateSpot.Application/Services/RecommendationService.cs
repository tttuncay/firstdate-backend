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
        // 1. Veritabanından Kullanıcının Seçtiği Ülke/Şehir/İlçeler bazlı aday havuzunu çek (15 - 30 mekan)
        var candidates = await _venueRepository.GetVenuesByFiltersAsync(
            country: request.Country,
            city: request.City,
            district: request.District,
            coveredDistricts: request.CoveredDistricts,
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
                    Message = "Maalesef seçilen ilçede kayıtlı mekan bulunamadı."
                };
            }
        }

        // 2. Ağırlıklı Puanlama Algoritması
        var scoredVenues = candidates.Select(venue =>
        {
            double score = CalculateFirstDateScore(venue, request);

            return new
            {
                Venue = venue,
                Score = score
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
                DistrictId = v.DistrictId,
                Name = v.Name,
                Country = v.Country,
                CountryCode = v.CountryCode,
                City = v.City,
                StateOrRegion = v.StateOrRegion,
                District = v.District,
                Neighborhood = v.Neighborhood,
                Address = v.Address,
                PostalCode = v.PostalCode,
                Currency = v.Currency,
                TimeZone = v.TimeZone,
                GoogleRating = v.GoogleRating,
                ReviewCount = v.ReviewCount,
                MatchScore = matchPercent,
                HeroImageUrl = v.HeroImageUrl,
                GalleryImages = v.GalleryImages,
                GoogleMapsUrl = v.GoogleMapsUrl,
                InstagramHandle = v.InstagramHandle,
                WebsiteUrl = v.WebsiteUrl,
                PhoneNumber = v.PhoneNumber,

                NoiseLevel = v.NoiseLevel,
                LightingStyle = v.LightingStyle,
                MusicProfile = v.MusicProfile,
                DressCode = v.DressCode,
                ViewType = v.ViewType,

                PriceLevel = v.PriceLevel,
                CuisineTypes = v.CuisineTypes,
                MealTimes = v.MealTimes,
                SignatureItems = v.SignatureItems,
                DietaryOptions = v.DietaryOptions,

                SeatingTypes = v.SeatingTypes,
                TableSpacing = v.TableSpacing,
                BestTableTip = string.IsNullOrWhiteSpace(v.BestTableTip) ? advice.TableTactics : v.BestTableTip,
                SeatingArrangement = v.SeatingArrangement,

                HasAlcohol = v.HasAlcohol,
                HasOutdoorSeating = v.HasOutdoorSeating,
                HasValetParking = v.HasValetParking,
                RequiresReservation = v.RequiresReservation,
                IsPetFriendly = v.IsPetFriendly,
                HasWifiAndSockets = v.HasWifiAndSockets,
                SmokingArea = v.SmokingArea,

                SuitableOccasions = v.SuitableOccasions,
                VibeTags = v.VibeTags,
                BestTimeToVisit = v.BestTimeToVisit,
                Advice = new DateAdviceDto
                {
                    WhyThisSpot = advice.WhyThisSpot,
                    IcebreakerTopic = advice.IcebreakerTopic,
                    TableTactics = !string.IsNullOrWhiteSpace(v.BestTableTip) ? v.BestTableTip : advice.TableTactics,
                    IdealOrderRecommendation = (v.SignatureItems != null && v.SignatureItems.Any()) 
                        ? string.Join(", ", v.SignatureItems) 
                        : advice.IdealOrderRecommendation
                }
            };
        }).ToList();

        return new RecommendationResponseDto
        {
            Success = true,
            TotalCandidatesAnalyzed = candidates.Count,
            RecommendedVenues = resultList,
            Message = $"{resultList.Count} harika mekan yapay zeka tarafından senin için hazırlandı."
        };
    }

    private static double CalculateFirstDateScore(Venue venue, RecommendationRequestDto req)
    {
        double score = 50.0; // Baz Puan

        // 1. Buluşma Amacı / Occasion Uyumluluğu (Max: +30 Puan)
        if (!string.IsNullOrWhiteSpace(req.Occasion))
        {
            var occ = req.Occasion.ToLower();
            bool matched = venue.SuitableOccasions.Any(o => 
                o.ToLower().Contains(occ) || occ.Contains(o.ToLower()) ||
                (occ.Contains("romantik") && o.ToLower().Contains("romantik")) ||
                (occ.Contains("iş") && (o.ToLower().Contains("iş") || o.ToLower().Contains("çalışma"))) ||
                (occ.Contains("kutlama") && (o.ToLower().Contains("kutlama") || o.ToLower().Contains("doğum"))) ||
                (occ.Contains("arkadaş") && (o.ToLower().Contains("arkadaş") || o.ToLower().Contains("eğlence"))) ||
                (occ.Contains("kafa dinleme") && (o.ToLower().Contains("kafa") || o.ToLower().Contains("kahve"))) ||
                (occ.Contains("aile") && (o.ToLower().Contains("aile") || o.ToLower().Contains("kahvaltı"))));

            if (matched)
            {
                score += 25.0;
            }
            else if (occ.Contains("piknik") || occ.Contains("doğa") || occ.Contains("açık hava") || occ.Contains("yürüyüş"))
            {
                bool isOutdoorSpot = venue.SuitableOccasions.Any(o => o.ToLower().Contains("açık hava") || o.ToLower().Contains("piknik") || o.ToLower().Contains("doğa"))
                    || venue.VibeTags.Any(v => v.ToLower().Contains("piknik") || v.ToLower().Contains("seyir tepesi") || v.ToLower().Contains("doğa"));
                
                if (isOutdoorSpot)
                    score += 35.0;
                else
                    score -= 15.0;
            }
            else if (venue.SuitableOccasions.Any())
            {
                score += 10.0;
            }
        }

        // 2. Mekan Tarzı Uyumluluğu (Max: +25 Puan)
        if (!string.IsNullOrWhiteSpace(req.VenueType))
        {
            var vt = req.VenueType.ToLower();
            var allVibeStr = string.Join(" ", venue.VibeTags).ToLower() + " " + string.Join(" ", venue.CuisineTypes).ToLower() + " " + venue.Name.ToLower();

            if (vt.Contains("piknik") || vt.Contains("doğa") || vt.Contains("seyir") || vt.Contains("koru") || vt.Contains("park"))
            {
                if (allVibeStr.Contains("piknik") || allVibeStr.Contains("seyir") || allVibeStr.Contains("doğa") || allVibeStr.Contains("koru") || allVibeStr.Contains("park") || allVibeStr.Contains("tepe") || allVibeStr.Contains("açık hava"))
                    score += 35.0;
                else
                    score -= 20.0;
            }
            else if (vt.Contains("kahve") || vt.Contains("kafe") || vt.Contains("tatlı"))
            {
                if (allVibeStr.Contains("kahve") || allVibeStr.Contains("tatlı") || allVibeStr.Contains("butik") || allVibeStr.Contains("kafe"))
                    score += 20.0;
            }
            else if (vt.Contains("kebap") || vt.Contains("ocakbaşı") || vt.Contains("et"))
            {
                if (allVibeStr.Contains("kebap") || allVibeStr.Contains("ocakbaşı") || allVibeStr.Contains("et") || allVibeStr.Contains("ızgara"))
                    score += 20.0;
            }
            else if (vt.Contains("restoran") || vt.Contains("dünya"))
            {
                if (allVibeStr.Contains("restoran") || allVibeStr.Contains("yemek") || allVibeStr.Contains("brasserie") || allVibeStr.Contains("makarna") || allVibeStr.Contains("pizza"))
                    score += 20.0;
            }
            else if (vt.Contains("meyhane") || vt.Contains("balık"))
            {
                if (venue.HasAlcohol && (allVibeStr.Contains("meyhane") || allVibeStr.Contains("balık") || allVibeStr.Contains("meze") || allVibeStr.Contains("rakı")))
                    score += 20.0;
            }
            else if (vt.Contains("pub") || vt.Contains("bar") || vt.Contains("kokteyl"))
            {
                if (venue.HasAlcohol && (allVibeStr.Contains("kokteyl") || allVibeStr.Contains("bar") || allVibeStr.Contains("pub") || allVibeStr.Contains("bira")))
                    score += 20.0;
            }
            else if (vt.Contains("teras") || vt.Contains("manzara") || vt.Contains("bahçe"))
            {
                if (venue.HasOutdoorSeating || allVibeStr.Contains("teras") || allVibeStr.Contains("manzara") || allVibeStr.Contains("boğaz") || allVibeStr.Contains("bahçe"))
                    score += 20.0;
            }
            else if (vt.Contains("burger") || vt.Contains("sokak"))
            {
                if (allVibeStr.Contains("burger") || allVibeStr.Contains("sokak") || allVibeStr.Contains("taco"))
                    score += 20.0;
            }
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
}
