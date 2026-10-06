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
            occasion: request.Occasion,
            venueType: request.VenueType,
            customPrompt: request.Prompt,
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

        // Eğer Gemini API anahtarı tanımsızsa veya istek zaman aşımına uğrarsa, en kaliteli adayları döndür
        var fallbackList = candidates.Take(10).Select((v, index) =>
        {
            double matchScore = Math.Round(98.0 - (index * 1.5), 1);
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
                MatchScore = matchScore,
                MatchBreakdown = new MatchBreakdownDto
                {
                    OccasionScore = Math.Round(matchScore + 0.5, 1),
                    VibeScore = Math.Round(matchScore - 0.5, 1),
                    SeatingScore = Math.Round(matchScore - 1.0, 1),
                    BudgetScore = Math.Round(matchScore, 1),
                    OverallMatch = matchScore
                },
                HeroImageUrl = v.HeroImageUrl,
                GalleryImages = v.GalleryImages ?? new(),
                GoogleMapsUrl = v.GoogleMapsUrl,
                InstagramHandle = v.InstagramHandle,
                WebsiteUrl = v.WebsiteUrl,
                PhoneNumber = v.PhoneNumber,

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
                    WhyThisSpot = $"{v.Name}, seçilen kriterler için kaliteli atmosferi ve yüksek puanıyla öne çıkmaktadır.",
                    IcebreakerTopic = "Mekanın ambiyansı ve semtin popüler noktaları üzerine sohbet açabilirsiniz.",
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
