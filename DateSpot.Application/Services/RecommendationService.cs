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
        // -------------------------------------------------------------------------
        // 1. AŞAMA: SQL Hızlı Filtreleme & Aday Havuzu (PostgreSQL Ön Elemesi)
        // 30.000 mekan arasından sert filtreler ve Bayesian puanıyla en iyi 35 adayı çeker
        // -------------------------------------------------------------------------
        var candidates = await _venueRepository.GetVenuesByFiltersAsync(
            country: request.Country,
            city: request.City,
            district: request.District,
            coveredDistricts: request.CoveredDistricts,
            maxPriceLevel: request.MaxPriceLevel,
            requiresAlcohol: request.AlcoholRequired,
            requiresParking: request.ParkingRequired,
            requiresOutdoor: request.OutdoorRequired,
            requiresPetFriendly: request.PetFriendlyRequired,
            timeSlot: !string.IsNullOrWhiteSpace(request.TimeSlot) ? request.TimeSlot : request.DateTiming,
            occasion: request.Occasion,
            venueType: request.VenueType,
            customPrompt: request.Prompt,
            noisePreference: request.NoisePreference,
            groupSize: request.GroupSize,
            candidateLimit: 40,
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
                    Message = "Maalesef seçilen kriterlerde kayıtlı mekan bulunamadı."
                };
            }
        }

        // -------------------------------------------------------------------------
        // 2. AŞAMA: LLM-as-a-Ranker (Gemini Doğrudan Sıralama ve Kürasyon Yapar)
        // -------------------------------------------------------------------------
        var aiCuratedList = await _geminiAdvisorService.RankAndCurateVenuesAsync(
            candidatePool: candidates,
            occasion: request.Occasion,
            requestedConcept: request.Concept,
            venueType: request.VenueType,
            groupSize: request.GroupSize,
            dateTiming: request.DateTiming,
            noisePreference: request.NoisePreference,
            district: request.District,
            customPrompt: request.Prompt,
            targetCount: 10,
            cancellationToken: cancellationToken);

        var candidateMap = candidates.ToDictionary(v => v.Id, v => v);

        // Doğrudan %100 Yapay Zeka (Gemini) Küratör Çıktısını Kullan
        if (aiCuratedList != null && aiCuratedList.Any())
        {
            var curatedResultList = new List<VenueRecommendationDto>();

            foreach (var aiItem in aiCuratedList)
            {
                if (candidateMap.TryGetValue(aiItem.Id, out var v))
                {
                    curatedResultList.Add(new VenueRecommendationDto
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
                        Latitude = v.Latitude,
                        Longitude = v.Longitude,
                        Currency = v.Currency,
                        TimeZone = v.TimeZone,
                        GoogleRating = v.GoogleRating,
                        ReviewCount = v.ReviewCount,
                        MatchScore = Math.Clamp(Math.Round(aiItem.MatchScore, 1), 78.0, 99.0),
                        MatchBreakdown = new MatchBreakdownDto
                        {
                            OccasionScore = Math.Round(Math.Clamp(aiItem.OccasionScore, 70.0, 99.0), 1),
                            VibeScore = Math.Round(Math.Clamp(aiItem.VibeScore, 70.0, 99.0), 1),
                            SeatingScore = Math.Round(Math.Clamp(aiItem.SeatingScore, 70.0, 99.0), 1),
                            BudgetScore = Math.Round(Math.Clamp(aiItem.BudgetScore, 70.0, 99.0), 1),
                            OverallMatch = Math.Round(Math.Clamp(aiItem.MatchScore, 78.0, 99.0), 1)
                        },
                        HeroImageUrl = v.HeroImageUrl,
                        GalleryImages = v.GalleryImages ?? new(),
                        GoogleMapsUrl = v.GoogleMapsUrl,
                        InstagramHandle = v.InstagramHandle,
                        WebsiteUrl = v.WebsiteUrl,
                        PhoneNumber = v.PhoneNumber,
                        IsOpenNow = true,
                        OpeningHoursText = !string.IsNullOrWhiteSpace(v.BestTimeToVisit) ? v.BestTimeToVisit : "09:00 - 23:30",

                        NoiseLevel = v.NoiseLevel,
                        LightingStyle = v.LightingStyle,
                        MusicProfile = v.MusicProfile,
                        DressCode = v.DressCode,
                        ViewType = v.ViewType ?? new(),

                        PriceLevel = v.PriceLevel,
                        CuisineTypes = v.CuisineTypes ?? new(),
                        MealTimes = v.MealTimes ?? new(),
                        SignatureItems = v.SignatureItems ?? new(),
                        DietaryOptions = v.DietaryOptions ?? new(),

                        SeatingTypes = v.SeatingTypes ?? new(),
                        TableSpacing = v.TableSpacing,
                        BestTableTip = !string.IsNullOrWhiteSpace(aiItem.TableTactics) ? aiItem.TableTactics : v.BestTableTip,
                        SeatingArrangement = v.SeatingArrangement,

                        HasAlcohol = v.HasAlcohol,
                        HasOutdoorSeating = v.HasOutdoorSeating,
                        HasValetParking = v.HasValetParking,
                        RequiresReservation = v.RequiresReservation,
                        IsPetFriendly = v.IsPetFriendly,
                        HasWifiAndSockets = v.HasWifiAndSockets,
                        SmokingArea = v.SmokingArea,

                        SuitableOccasions = v.SuitableOccasions ?? new(),
                        VibeTags = v.VibeTags ?? new(),
                        BestTimeToVisit = v.BestTimeToVisit,
                        Advice = new DateAdviceDto
                        {
                            WhyThisSpot = aiItem.WhyThisSpot,
                            IcebreakerTopic = aiItem.IcebreakerTopic,
                            TableTactics = !string.IsNullOrWhiteSpace(aiItem.TableTactics) ? aiItem.TableTactics : v.BestTableTip,
                            IdealOrderRecommendation = !string.IsNullOrWhiteSpace(aiItem.IdealOrderRecommendation) 
                                ? aiItem.IdealOrderRecommendation 
                                : ((v.SignatureItems != null && v.SignatureItems.Any()) ? string.Join(", ", v.SignatureItems.Take(2)) : "Günün spesiyali.")
                        }
                    });
                }
            }

            if (curatedResultList.Any())
            {
                return new RecommendationResponseDto
                {
                    Success = true,
                    TotalCandidatesAnalyzed = candidates.Count,
                    RecommendedVenues = curatedResultList,
                    Message = $"{curatedResultList.Count} mekan yapay zeka küratörü tarafından senin için özel olarak seçildi ve sıralandı."
                };
            }
        }

        // Eğer Gemini API anahtarı tanımsızsa veya istek zaman aşımına uğrarsa, çok boyutlu algoritmik puanlamayla en kaliteli adayları döndür
        var fallbackList = candidates.Take(10).Select((v, index) =>
        {
            var occLower = (request.Occasion ?? string.Empty).ToLower();
            var occasionsStr = string.Join(" ", v.SuitableOccasions ?? new()).ToLower();
            bool hasOccasionMatch = !string.IsNullOrWhiteSpace(occLower) && occasionsStr.Contains(occLower.Split(' ')[0]);

            double baseRankScore = 97.5 - (index * 1.3);
            double ratingBoost = Math.Clamp((v.GoogleRating - 4.4) * 3.0, -1.5, 1.8);
            double overallScore = Math.Clamp(Math.Round(baseRankScore + ratingBoost, 1), 82.0, 99.0);

            double occScore = Math.Clamp(Math.Round(overallScore + (hasOccasionMatch ? 1.2 : -1.0), 1), 80.0, 99.0);
            double vibeScore = Math.Clamp(Math.Round(overallScore + (request.NoisePreference.HasValue && (int)v.NoiseLevel == request.NoisePreference.Value ? 1.4 : -0.6), 1), 80.0, 99.0);
            double seatingScore = Math.Clamp(Math.Round(overallScore + (!string.IsNullOrEmpty(v.TableSpacing) && v.TableSpacing.Contains("Geniş", StringComparison.OrdinalIgnoreCase) ? 1.5 : -0.5), 1), 80.0, 99.0);
            double budgetScore = Math.Clamp(Math.Round(overallScore + (!request.MaxPriceLevel.HasValue || v.PriceLevel <= request.MaxPriceLevel.Value ? 1.0 : -3.0), 1), 80.0, 99.0);

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
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                Currency = v.Currency,
                TimeZone = v.TimeZone,
                GoogleRating = v.GoogleRating,
                ReviewCount = v.ReviewCount,
                MatchScore = overallScore,
                MatchBreakdown = new MatchBreakdownDto
                {
                    OccasionScore = occScore,
                    VibeScore = vibeScore,
                    SeatingScore = seatingScore,
                    BudgetScore = budgetScore,
                    OverallMatch = overallScore
                },
                HeroImageUrl = v.HeroImageUrl,
                GalleryImages = v.GalleryImages ?? new(),
                GoogleMapsUrl = v.GoogleMapsUrl,
                InstagramHandle = v.InstagramHandle,
                WebsiteUrl = v.WebsiteUrl,
                PhoneNumber = v.PhoneNumber,
                IsOpenNow = true,
                OpeningHoursText = !string.IsNullOrWhiteSpace(v.BestTimeToVisit) ? v.BestTimeToVisit : "09:00 - 23:30",

                NoiseLevel = v.NoiseLevel,
                LightingStyle = v.LightingStyle,
                MusicProfile = v.MusicProfile,
                DressCode = v.DressCode,
                ViewType = v.ViewType ?? new(),

                PriceLevel = v.PriceLevel,
                CuisineTypes = v.CuisineTypes ?? new(),
                MealTimes = v.MealTimes ?? new(),
                SignatureItems = v.SignatureItems ?? new(),
                DietaryOptions = v.DietaryOptions ?? new(),

                SeatingTypes = v.SeatingTypes ?? new(),
                TableSpacing = v.TableSpacing,
                BestTableTip = !string.IsNullOrWhiteSpace(v.BestTableTip) ? v.BestTableTip : "Rahat bir masa tercih edin.",
                SeatingArrangement = v.SeatingArrangement,

                HasAlcohol = v.HasAlcohol,
                HasOutdoorSeating = v.HasOutdoorSeating,
                HasValetParking = v.HasValetParking,
                RequiresReservation = v.RequiresReservation,
                IsPetFriendly = v.IsPetFriendly,
                HasWifiAndSockets = v.HasWifiAndSockets,
                SmokingArea = v.SmokingArea,

                SuitableOccasions = v.SuitableOccasions ?? new(),
                VibeTags = v.VibeTags ?? new(),
                BestTimeToVisit = v.BestTimeToVisit,
                Advice = new DateAdviceDto
                {
                    WhyThisSpot = !string.IsNullOrWhiteSpace(request.Occasion)
                        ? $"{v.Name}, '{request.Occasion}' planın için {(!string.IsNullOrWhiteSpace(v.LightingStyle) ? v.LightingStyle.ToLower() + " atmosferi, " : "")}{v.TableSpacing.ToLower()} oturma düzeni ve {v.GoogleRating:F1} memnuniyet puanıyla öne çıkıyor."
                        : $"{v.Name}, seçilen kriterler için kaliteli atmosferi ve yüksek puanıyla öne çıkmaktadır.",
                    IcebreakerTopic = "Mekanın imza lezzetleri ve semtin keyifli atmosferi üzerine sohbet açabilirsiniz.",
                    TableTactics = !string.IsNullOrWhiteSpace(v.BestTableTip) ? v.BestTableTip : "Girişten uzak ve rahat konuşabileceğiniz konforlu bir köşe masa tercih edin.",
                    IdealOrderRecommendation = (v.SignatureItems != null && v.SignatureItems.Any()) ? string.Join(", ", v.SignatureItems.Take(2)) : "Günün spesiyali ve meşhur içeceği."
                }
            };
        }).ToList();

        return new RecommendationResponseDto
        {
            Success = true,
            TotalCandidatesAnalyzed = candidates.Count,
            RecommendedVenues = fallbackList,
            Message = $"{fallbackList.Count} mekan başarıyla listelendi."
        };
    }
}
