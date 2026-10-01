using DateSpot.Core.Enums;

namespace DateSpot.Application.DTOs;

public class RecommendationRequestDto
{
    public string District { get; set; } = string.Empty; // "Kadıköy", "Beşiktaş", "Beyoğlu", "Tüm İstanbul", vb.
    public List<string> CoveredDistricts { get; set; } = new(); // Çemberin kapsadığı tüm ilçeler
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 10.0;
    public DateConcept Concept { get; set; } = DateConcept.RomanticAndChic;

    public string VenueType { get; set; } = string.Empty; // "Butik & 3. Nesil Kahveci", "Klasik Kahve Zinciri", "Sahil Kenarı", "Kokteyl Bar & Pub", "Şarap Evi & Romantik Mahzen", "Şık Restoran & Akşam Yemeği"
    public int? NoisePreference { get; set; } // 1: Sakin, 2: Dengeli, 3: Canlı

    public string VibeArchetype { get; set; } = string.Empty;
    public string MusicStyle { get; set; } = string.Empty;
    public string SeatingPrivacy { get; set; } = string.Empty;
    public string LightingStyle { get; set; } = string.Empty;

    public PriceLevel? MaxPriceLevel { get; set; } = PriceLevel.Moderate;
    public bool? AlcoholRequired { get; set; }
    public bool? ParkingRequired { get; set; }
    public string? AppUserId { get; set; } // RevenueCat subscriber ID
}

public class VenueRecommendationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceInKm { get; set; }
    
    public PriceLevel PriceLevel { get; set; }
    public NoiseLevel NoiseLevel { get; set; }
    public List<string> VibeTags { get; set; } = new();
    public string SeatingArrangement { get; set; } = string.Empty;
    public bool HasAlcohol { get; set; }
    public bool HasValetParking { get; set; }
    public bool RequiresReservation { get; set; }
    public bool HasOutdoorSeating { get; set; }
    public double GoogleRating { get; set; }
    public double MatchScore { get; set; } // 0 - 100%

    public string HeroImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;

    // AI Curation Fields
    public DateAdviceDto Advice { get; set; } = new();
}

public class DateAdviceDto
{
    public string WhyThisSpot { get; set; } = string.Empty;
    public string IcebreakerTopic { get; set; } = string.Empty;
    public string TableTactics { get; set; } = string.Empty;
    public string IdealOrderRecommendation { get; set; } = string.Empty;
}

public class RecommendationResponseDto
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public int TotalCandidatesAnalyzed { get; set; }
    public List<VenueRecommendationDto> RecommendedVenues { get; set; } = new();
}
