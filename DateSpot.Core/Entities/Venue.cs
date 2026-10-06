using DateSpot.Core.Enums;

namespace DateSpot.Core.Entities;

public class Venue
{
    // ============================================================
    // 1. TEMEL KİMLİK & LOKASYON (Core Identity & Global Location)
    // ============================================================
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? GooglePlaceId { get; set; } // Google Places API Unique ID (Mükerrer kaydı engeller)
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = "Türkiye";
    public string CountryCode { get; set; } = "TR"; // ISO 3166-1 alpha-2 (TR, US, GB, DE, FR, JP, IT, vb.)
    public string City { get; set; } = "İstanbul"; // İstanbul, New York, London, Paris, Tokyo, vb.
    public string StateOrRegion { get; set; } = string.Empty; // Marmara, New York State, Île-de-France, vb.
    public int? DistrictId { get; set; } // Opsiyonel Foreign Key
    public District? DistrictRef { get; set; } // Navigation Property
    public string District { get; set; } = string.Empty; // Kadıköy, Üsküdar, Manhattan, Westminster, vb.
    public string Neighborhood { get; set; } = string.Empty; // Kuzguncuk, SoHo, Mayfair, Le Marais, vb.
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Currency { get; set; } = "TRY"; // TRY, USD, EUR, GBP, JPY
    public string TimeZone { get; set; } = "Europe/Istanbul"; // Europe/Istanbul, America/New_York, Europe/London
    public double GoogleRating { get; set; } = 4.5;
    public int ReviewCount { get; set; } = 100;
    public string HeroImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;
    public string? WebsiteUrl { get; set; }
    public string? PhoneNumber { get; set; }

    // ============================================================
    // 2. AMBİYANS & DUYUSAL PROFİL (Sensory & Vibe)
    // ============================================================
    public NoiseLevel NoiseLevel { get; set; } = NoiseLevel.ModerateMusic;
    public string LightingStyle { get; set; } = "Sıcak Sarı"; // "Loş & Mum Işığı", "Sıcak Sarı", "Ferah Gün Işığı", vb.
    public string MusicProfile { get; set; } = "Caz & Akustik (Düşük Arka Plan)";
    public string DressCode { get; set; } = "Casual"; // "Casual", "Smart Casual", "Chic & Gece"
    public List<string> ViewType { get; set; } = new(); // ["Boğaz Manzarası", "Deniz Sıfır", "Tarihi Sokak", "Yeşil Bahçe"]

    // ============================================================
    // 3. MUTFAK, MENÜ & İMZA LEZZETLER (Culinary Profile)
    // ============================================================
    public PriceLevel PriceLevel { get; set; } = PriceLevel.Moderate;
    public List<string> CuisineTypes { get; set; } = new(); // ["3. Nesil Kahve", "İtalyan", "Deniz Ürünleri", "Kokteyl Bar"]
    public List<string> MealTimes { get; set; } = new(); // ["Serpme Kahvaltı", "Öğle Yemeği", "Kahve & Tatlı", "Akşam Yemeği"]
    public List<string> SignatureItems { get; set; } = new(); // ["San Sebastian Cheesecake", "Truffle Tagliolini", "Smoky Negroni"]
    public List<string> DietaryOptions { get; set; } = new(); // ["Vejetaryen Dostu", "Vegan Seçenekler", "Glütensiz Alternatifler"]

    // ============================================================
    // 4. MASA, OTURMA & MEKANSAL TAKTİKLER (Spatial Intelligence)
    // ============================================================
    public List<string> SeatingTypes { get; set; } = new(); // ["L-Koltuk", "Bar Tabureleri", "Cam Kenarı İkili Masa", "İzole Bahçe"]
    public string TableSpacing { get; set; } = "Ferah"; // "Geniş & Mahrem", "Standart", "Bitişik & Sosyal"
    public string BestTableTip { get; set; } = string.Empty; // Editör/AI Masa Taktiği
    public string SeatingArrangement { get; set; } = string.Empty;

    // ============================================================
    // 5. PRATİK KOLAYLIKLAR & LOJİSTİK (Convenience)
    // ============================================================
    public bool HasAlcohol { get; set; }
    public bool HasOutdoorSeating { get; set; }
    public bool HasValetParking { get; set; }
    public bool RequiresReservation { get; set; }
    public bool IsPetFriendly { get; set; }
    public bool HasWifiAndSockets { get; set; }
    public string SmokingArea { get; set; } = "Bahçe"; // "Açık Bahçede Serbest", "Tamamen Sigarasız", "Teras Bölümü"

    // ============================================================
    // 6. AI, BULUŞMA AMAÇLARI & GELECEK GÜVENCESİ (AI Layer)
    // ============================================================
    public List<string> SuitableOccasions { get; set; } = new(); // ["İlk Buluşma & Romantik", "İş Toplantısı & Freelance", ...]
    public List<string> VibeTags { get; set; } = new(); // ["Mum Işığı", "Tarihi Doku", "Entelektüel", "Fotojenik"]
    public string BestTimeToVisit { get; set; } = string.Empty; // "Hafta içi 14:00 - 17:00 arası"
    public double FirstDateSuitabilityScore { get; set; } = 8.5;
    public string RawMetadata { get; set; } = "{}"; // JSON Deposu
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
