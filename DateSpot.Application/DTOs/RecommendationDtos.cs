using DateSpot.Core.Enums;

namespace DateSpot.Application.DTOs;

public class RecommendationRequestDto
{
    public string Country { get; set; } = "Türkiye";
    public string CountryCode { get; set; } = "TR";
    public string City { get; set; } = "İstanbul";
    public string Zone { get; set; } = string.Empty; // "Anadolu Yakası", "Avrupa Yakası", "Downtown", "West End", "Tümü"
    public string Side { get => Zone; set => Zone = value; } // Geriye dönük uyumluluk köprüsü
    public string District { get; set; } = string.Empty; // "Kadıköy", "Beşiktaş", "Üsküdar", "Manhattan", vb.
    public List<string> CoveredDistricts { get; set; } = new(); // Çemberin kapsadığı tüm ilçeler/bölgeler
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double RadiusInKm { get; set; } = 10.0;
    public DateConcept Concept { get; set; } = DateConcept.RomanticAndChic;
    public string Occasion { get; set; } = string.Empty; // "İlk Buluşma & Romantik", "İş Buluşması & Çalışma", "Kutlama & Doğum Günü", "Arkadaşlarla Muhabbet", "Tek Başına Kafa Dinleme", "Aile Yemeği"
    public string EventType { get; set; } = string.Empty;
    public string VenueType { get; set; } = string.Empty; // "Butik & 3. Nesil Kahveci", "Klasik Kahve Zinciri", "Sahil Kenarı", "Kokteyl Bar & Pub", "Şarap Evi & Romantik Mahzen", "Şık Restoran & Akşam Yemeği"
    public int? NoisePreference { get; set; } // 1: Sakin, 2: Dengeli, 3: Canlı

    public string VibeArchetype { get; set; } = string.Empty;
    public string MusicStyle { get; set; } = string.Empty;
    public string SeatingPrivacy { get; set; } = string.Empty;
    public string LightingStyle { get; set; } = string.Empty;

    public PriceLevel? MaxPriceLevel { get; set; } = PriceLevel.Moderate;
    public bool? AlcoholRequired { get; set; }
    public bool? ParkingRequired { get; set; }
    public bool? ReservationPreferred { get; set; }
    public string GroupSize { get; set; } = string.Empty; // "1-2 Kişi", "3-5 Kişi", "6+ Kişi"
    public string? AppUserId { get; set; } // RevenueCat subscriber ID
}

public class VenueRecommendationDto
{
    // 1. Kimlik & Lokasyon (Global)
    public Guid Id { get; set; }
    public int? DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = "Türkiye";
    public string CountryCode { get; set; } = "TR";
    public string City { get; set; } = "İstanbul";
    public string StateOrRegion { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public double GoogleRating { get; set; }
    public int ReviewCount { get; set; }
    public double MatchScore { get; set; } // 0 - 100%
    public string HeroImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? PhoneNumber { get; set; }

    // 2. Ambiyans & Duyusal Profil
    public NoiseLevel NoiseLevel { get; set; }
    public string LightingStyle { get; set; } = "Sıcak Sarı";
    public string MusicProfile { get; set; } = "Caz & Akustik";
    public string DressCode { get; set; } = "Casual";
    public List<string> ViewType { get; set; } = new();

    // 3. Mutfak, Menü & İmza Lezzetler
    public PriceLevel PriceLevel { get; set; }
    public List<string> CuisineTypes { get; set; } = new();
    public List<string> MealTimes { get; set; } = new();
    public List<string> SignatureItems { get; set; } = new();
    public List<string> DietaryOptions { get; set; } = new();

    // 4. Masa, Oturma & Mekansal Taktikler
    public List<string> SeatingTypes { get; set; } = new();
    public string TableSpacing { get; set; } = "Ferah";
    public string BestTableTip { get; set; } = string.Empty;
    public string SeatingArrangement { get; set; } = string.Empty;

    // 5. Pratik Kolaylıklar & Lojistik
    public bool HasAlcohol { get; set; }
    public bool HasOutdoorSeating { get; set; }
    public bool HasValetParking { get; set; }
    public bool RequiresReservation { get; set; }
    public bool IsPetFriendly { get; set; }
    public bool HasWifiAndSockets { get; set; }
    public string SmokingArea { get; set; } = "Bahçe";

    // 6. AI, Buluşma Amaçları & Tavsiye
    public List<string> SuitableOccasions { get; set; } = new();
    public List<string> VibeTags { get; set; } = new();
    public string BestTimeToVisit { get; set; } = string.Empty;
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

public class CreateVenueDto
{
    public string Name { get; set; } = string.Empty;
    public int? DistrictId { get; set; }
    public string Country { get; set; } = "Türkiye";
    public string CountryCode { get; set; } = "TR";
    public string City { get; set; } = "İstanbul";
    public string StateOrRegion { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
    public string TimeZone { get; set; } = "Europe/Istanbul";
    public double GoogleRating { get; set; } = 4.5;
    public int ReviewCount { get; set; } = 100;
    public string HeroImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? PhoneNumber { get; set; }

    public NoiseLevel NoiseLevel { get; set; } = NoiseLevel.ModerateMusic;
    public string LightingStyle { get; set; } = "Sıcak Sarı";
    public string MusicProfile { get; set; } = "Caz & Akustik";
    public string DressCode { get; set; } = "Casual";
    public List<string> ViewType { get; set; } = new();

    public PriceLevel PriceLevel { get; set; } = PriceLevel.Moderate;
    public List<string> CuisineTypes { get; set; } = new();
    public List<string> MealTimes { get; set; } = new();
    public List<string> SignatureItems { get; set; } = new();
    public List<string> DietaryOptions { get; set; } = new();

    public List<string> SeatingTypes { get; set; } = new();
    public string TableSpacing { get; set; } = "Ferah";
    public string BestTableTip { get; set; } = string.Empty;
    public string SeatingArrangement { get; set; } = string.Empty;

    public bool HasAlcohol { get; set; }
    public bool HasOutdoorSeating { get; set; }
    public bool HasValetParking { get; set; }
    public bool RequiresReservation { get; set; }
    public bool IsPetFriendly { get; set; }
    public bool HasWifiAndSockets { get; set; }
    public string SmokingArea { get; set; } = "Bahçe";

    public List<string> SuitableOccasions { get; set; } = new();
    public List<string> VibeTags { get; set; } = new();
    public string BestTimeToVisit { get; set; } = string.Empty;
    public double FirstDateSuitabilityScore { get; set; } = 8.5;
    public string RawMetadata { get; set; } = "{}";
}
