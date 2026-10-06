using Microsoft.EntityFrameworkCore;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;

namespace DateSpot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedVenuesAsync(DateSpotDbContext context)
    {
        
        // 1. İstanbul'un Tüm 39 İlçesini Tohumla (Seed All 39 Districts)
        if (!await context.Districts.AnyAsync())
        {
            var districts = new List<District>
            {
                // --- ANADOLU YAKASI (14 İlçe) ---
                new District { Name = "Kadıköy", Side = "Anadolu", PopularNeighborhoods = new() { "Moda", "Caferağa", "Caddebostan", "Suadiye", "Fenerbahçe", "Bostancı" } },
                new District { Name = "Üsküdar", Side = "Anadolu", PopularNeighborhoods = new() { "Kuzguncuk", "Beylerbeyi", "Çengelköy", "Kandilli", "Salacak" } },
                new District { Name = "Ataşehir", Side = "Anadolu", PopularNeighborhoods = new() { "Batı Ataşehir", "Atatürk", "Barbaros", "İçerenköy" } },
                new District { Name = "Maltepe", Side = "Anadolu", PopularNeighborhoods = new() { "Küçükyalı", "İdealtepe", "Yalı", "Bağlarbaşı" } },
                new District { Name = "Kartal", Side = "Anadolu", PopularNeighborhoods = new() { "Kordonboyu", "Atalar", "Yakacık", "Dragos" } },
                new District { Name = "Pendik", Side = "Anadolu", PopularNeighborhoods = new() { "Pendik Sahil", "Batı", "Kurtköy", "Yenişehir" } },
                new District { Name = "Tuzla", Side = "Anadolu", PopularNeighborhoods = new() { "Tuzla Marina", "Postane", "Cami", "İstasyon" } },
                new District { Name = "Ümraniye", Side = "Anadolu", PopularNeighborhoods = new() { "Atakent", "Yamanevler", "İnkılap", "Armağanevler" } },
                new District { Name = "Beykoz", Side = "Anadolu", PopularNeighborhoods = new() { "Kanlıca", "Anadoluhisarı", "Göksu", "Polonezköy", "Riva" } },
                new District { Name = "Çekmeköy", Side = "Anadolu", PopularNeighborhoods = new() { "Mimar Sinan", "Merkez", "Taşdelen" } },
                new District { Name = "Sancaktepe", Side = "Anadolu", PopularNeighborhoods = new() { "Samandıra", "Sarıgazi", "Abdurrahmangazi" } },
                new District { Name = "Sultanbeyli", Side = "Anadolu", PopularNeighborhoods = new() { "Abdurrahmangazi", "Fatih", "Mehmet Akif" } },
                new District { Name = "Şile", Side = "Anadolu", PopularNeighborhoods = new() { "Şile Sahil", "Ağva", "Balibey" } },
                new District { Name = "Adalar", Side = "Anadolu", PopularNeighborhoods = new() { "Büyükada", "Heybeliada", "Burgazada", "Kınalıada" } },

                // --- AVRUPA YAKASI (25 İlçe) ---
                new District { Name = "Beşiktaş", Side = "Avrupa", PopularNeighborhoods = new() { "Akaretler", "Arnavutköy", "Bebek", "Ortaköy", "Kuruçeşme", "Levent" } },
                new District { Name = "Beyoğlu", Side = "Avrupa", PopularNeighborhoods = new() { "Cihangir", "Karaköy", "Galata", "Asmalımescit", "Tomtom", "Gümüşsuyu" } },
                new District { Name = "Şişli", Side = "Avrupa", PopularNeighborhoods = new() { "Nişantaşı", "Teşvikiye", "Bomonti", "Harbiye", "Fulya", "Esentepe" } },
                new District { Name = "Sarıyer", Side = "Avrupa", PopularNeighborhoods = new() { "Yeniköy", "Emirgan", "İstinye", "Tarabya", "Rumeli Hisarı", "Kilyos" } },
                new District { Name = "Fatih", Side = "Avrupa", PopularNeighborhoods = new() { "Balat", "Fener", "Sultanahmet", "Sirkeci", "Eminönü", "Aksaray" } },
                new District { Name = "Bakırköy", Side = "Avrupa", PopularNeighborhoods = new() { "Yeşilköy", "Florya", "Ataköy", "Zuhuratbaba", "Yeşilyurt" } },
                new District { Name = "Kağıthane", Side = "Avrupa", PopularNeighborhoods = new() { "Seyrantepe", "Çeliktepe", "Emniyetevleri", "Merkez" } },
                new District { Name = "Eyüpsultan", Side = "Avrupa", PopularNeighborhoods = new() { "Göktürk", "Kemerburgaz", "Pierre Loti", "Merkez", "Alibeyköy" } },
                new District { Name = "Zeytinburnu", Side = "Avrupa", PopularNeighborhoods = new() { "Kazlıçeşme Sahil", "Merkezefendi", "Seyitnizam" } },
                new District { Name = "Gaziosmanpaşa", Side = "Avrupa", PopularNeighborhoods = new() { "Merkez", "Bağlarbaşı", "Karadeniz" } },
                new District { Name = "Bayrampaşa", Side = "Avrupa", PopularNeighborhoods = new() { "Kocatepe", "Muratpaşa", "Yenidogan" } },
                new District { Name = "Esenler", Side = "Avrupa", PopularNeighborhoods = new() { "Dörtyol", "Menderes", "Birlik" } },
                new District { Name = "Güngören", Side = "Avrupa", PopularNeighborhoods = new() { "Merter", "Akıncılar", "Güven" } },
                new District { Name = "Bağcılar", Side = "Avrupa", PopularNeighborhoods = new() { "Güneşli", "Mahmutbey", "Hürriyet" } },
                new District { Name = "Bahçelievler", Side = "Avrupa", PopularNeighborhoods = new() { "Yayla", "Basın Sitesi", "Şirinevler", "Yenibosna" } },
                new District { Name = "Küçükçekmece", Side = "Avrupa", PopularNeighborhoods = new() { "Cennet", "Atakent", "Halkalı", "Göl Kenarı" } },
                new District { Name = "Avcılar", Side = "Avrupa", PopularNeighborhoods = new() { "Ambarlı Sahil", "Denizköşkler", "Merkez" } },
                new District { Name = "Başakşehir", Side = "Avrupa", PopularNeighborhoods = new() { "Bahçeşehir", "Gölet", "Başakşehir 1. Etap", "Kayaşehir" } },
                new District { Name = "Beylikdüzü", Side = "Avrupa", PopularNeighborhoods = new() { "Beylikdüzü Marina", "Cumhuriyet", "Barış", "Yakuplu" } },
                new District { Name = "Esenyurt", Side = "Avrupa", PopularNeighborhoods = new() { "Cumhuriyet", "Mehterçeşme", "Güzelyurt" } },
                new District { Name = "Büyükçekmece", Side = "Avrupa", PopularNeighborhoods = new() { "Kordonboyu", "Mimaroba", "Sinanoba", "Kumburgaz" } },
                new District { Name = "Sultangazi", Side = "Avrupa", PopularNeighborhoods = new() { "Uğur Mumcu", "Sultançiftliği", "Gazi" } },
                new District { Name = "Arnavutköy", Side = "Avrupa", PopularNeighborhoods = new() { "Merkez", "Hadımköy", "Bolluca", "Karaburun" } },
                new District { Name = "Çatalca", Side = "Avrupa", PopularNeighborhoods = new() { "Kaleiçi", "Ferhatpaşa", "Binkılıç" } },
                new District { Name = "Silivri", Side = "Avrupa", PopularNeighborhoods = new() { "Silivri Sahil", "Piri Mehmet Paşa", "Selimpaşa" } }
            };

            await context.Districts.AddRangeAsync(districts);
            await context.SaveChangesAsync();
        }

        // 2. İstanbul First Date Mekanlarını Tohumla (Zengin & Gerçek Mekan Havuzu)
        if (!await context.Venues.AnyAsync())
        {
            var venues = GetAllInitialVenues();
            await context.Venues.AddRangeAsync(venues);
            await context.SaveChangesAsync();
        }
    }

    public static List<Venue> GetAllInitialVenues()
    {
                return new List<Venue>
        {
                // ==========================================
                // --- 1. BUTİK & 3. NESİL KAHVECİLER ---
                // ==========================================
                new Venue
                {
                    Name = "Minoa Books & Coffee",
                    District = "Beşiktaş",
                    Neighborhood = "Akaretler",
                    Address = "Vişnezade Mah. Süleyman Seba Cad. No:52/A, Akaretler, Beşiktaş",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "�lk Bulu�ma & Romantik", "�� Toplant�s� & Freelance �al��ma" },
                    VibeTags = new() { "Kitap & Sanat", "Entelektüel", "Sessiz Arka Bahçe", "Nitelikli Kahve", "Butik" },
                    SeatingArrangement = "Kitap rafları arasında ahşap masalar ve kış bahçesi",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.7,
                    GoogleRating = 4.8,
                    ReviewCount = 4200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1521587760476-6c12a4b040da?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Minoa+Akaretler",
                    InstagramHandle = "@minoabooksandcoffee"
                },
                new Venue
                {
                    Name = "Petra Roasting Co.",
                    District = "Beşiktaş",
                    Neighborhood = "Gayrettepe",
                    Address = "Gayrettepe Mah. Hoşsohbet Sok. No:1, Beşiktaş",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "3. Nesil Kahve", "Endüstriyel Tasarım", "Sanat Galerisi", "Ferah Tavan" },
                    SeatingArrangement = "Geniş ferah ortak masalar ve ikili rahat berjerler",
                    HasAlcohol = false,
                    HasValetParking = true,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.3,
                    GoogleRating = 4.7,
                    ReviewCount = 2800,
                    HeroImageUrl = "https://images.unsplash.com/photo-1554118811-1e0d58224f24?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Petra+Roasting+Gayrettepe",
                    InstagramHandle = "@petracooffee"
                },
                new Venue
                {
                    Name = "Story Coffee Roasters",
                    District = "Kadıköy",
                    Neighborhood = "Moda",
                    Address = "Caferağa Mah. Dalga Sok. No:2/A, Moda, Kadıköy",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "�lk Bulu�ma & Romantik", "�� Toplant�s� & Freelance �al��ma" },
                    VibeTags = new() { "Nitelikli Kahve", "Sessiz Bahçe", "Moda Sahili", "Tatlı Sohbet", "Butik" },
                    SeatingArrangement = "Ağaç altı sakin masalar ve ferah iç mekan",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.2,
                    GoogleRating = 4.6,
                    ReviewCount = 1450,
                    HeroImageUrl = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Story+Coffee+Moda",
                    InstagramHandle = "@storycoffeeroasters"
                },
                new Venue
                {
                    Name = "Montag Coffee Roasters",
                    District = "Kadıköy",
                    Neighborhood = "Caferağa / Çarşı",
                    Address = "Caferağa Mah. Muvakkıthane Cad. No:16/A Kat:1, Kadıköy",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Özel Kavrum", "Balkon Manzarası", "Genç & Dinamik", "3. Nesil Kahve" },
                    SeatingArrangement = "Meydana bakan tatlı balkon ve samimi ahşap masalar",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.9,
                    GoogleRating = 4.6,
                    ReviewCount = 2200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1497636577773-f1231844b336?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Montag+Coffee+Kadikoy",
                    InstagramHandle = "@montagcoffee"
                },
                new Venue
                {
                    Name = "Spada Coffee",
                    District = "Şişli",
                    Neighborhood = "Teşvikiye / Nişantaşı",
                    Address = "Teşvikiye Mah. Fırın Sok. No:1, Nişantaşı, Şişli",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Nitelikli Espresso", "Nişantaşı Sokak Havası", "Trend & Havalı", "Butik" },
                    SeatingArrangement = "Sokak cepheli açık hava oturma alanı ve modern iç köşe",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.0,
                    GoogleRating = 4.6,
                    ReviewCount = 1300,
                    HeroImageUrl = "https://images.unsplash.com/photo-1442512595331-e89e73853f31?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Spada+Coffee+Tesvikiye",
                    InstagramHandle = "@spadacoffee"
                },
                new Venue
                {
                    Name = "Nail Kitabevi & Cafe",
                    District = "Üsküdar",
                    Neighborhood = "Kuzguncuk",
                    Address = "Kuzguncuk Mah. İcadiye Cad. No:32, Üsküdar",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "�lk Bulu�ma & Romantik", "�� Toplant�s� & Freelance �al��ma" },
                    VibeTags = new() { "Tarihi Cumbalı Bina", "Kuzguncuk Masalı", "Kitap Kokusu", "Huzurlu & Butik" },
                    SeatingArrangement = "Cumbalı üst kat pencere önü masaları ve sokak önü",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.4,
                    GoogleRating = 4.7,
                    ReviewCount = 3100,
                    HeroImageUrl = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Nail+Kitabevi+Kuzguncuk",
                    InstagramHandle = "@nail_kitabevi"
                },
                new Venue
                {
                    Name = "Coffee Department Balat",
                    District = "Fatih",
                    Neighborhood = "Balat",
                    Address = "Ayvansaray Mah. Kürkçü Çeşmesi Sok. No:5/A, Balat, Fatih",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "�lk Bulu�ma & Romantik", "�� Toplant�s� & Freelance �al��ma" },
                    VibeTags = new() { "Tarihi Balat Sokakları", "Artizan Kahve", "Sıcak & Samimi", "Fotojenik" },
                    SeatingArrangement = "Arnavut kaldırımlı sokak masaları ve rustik iç mekan",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.1,
                    GoogleRating = 4.7,
                    ReviewCount = 1750,
                    HeroImageUrl = "https://images.unsplash.com/photo-1498804103079-a6351b050096?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Coffee+Department+Balat",
                    InstagramHandle = "@coffeedepartment"
                },

                // ==========================================
                // --- 2. KLASİK KAHVE ZİNCİRLERİ ---
                // ==========================================
                new Venue
                {
                    Name = "Espressolab Roastery Caddebostan",
                    District = "Kadıköy",
                    Neighborhood = "Caddebostan",
                    Address = "Caddebostan Mah. Bağdat Cad. No:298, Kadıköy",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Klasik Zincir", "Geniş Bahçe", "Bağdat Caddesi", "Rahat & Hızlı", "Sıfır Gerginlik" },
                    SeatingArrangement = "Büyük açık hava bahçe alanı ve ferah koltuklar",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.5,
                    GoogleRating = 4.4,
                    ReviewCount = 3800,
                    HeroImageUrl = "https://images.unsplash.com/photo-1509042239860-f550ce710b93?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Espressolab+Caddebostan",
                    InstagramHandle = "@espressolabtr"
                },
                new Venue
                {
                    Name = "Starbucks Reserve Bebek",
                    District = "Beşiktaş",
                    Neighborhood = "Bebek",
                    Address = "Bebek Mah. Cevdet Paşa Cad. No:30, Beşiktaş",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Boğaz Kıyısı", "Klasik Zincir", "Reserve Özel Seri", "Bebek Sahili" },
                    SeatingArrangement = "Boğaza nazır üst teras ve geniş cam kenarı masaları",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.7,
                    GoogleRating = 4.5,
                    ReviewCount = 5900,
                    HeroImageUrl = "https://images.unsplash.com/photo-1453614512568-c4024d13c247?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Starbucks+Reserve+Bebek",
                    InstagramHandle = "@starbucks_tr"
                },
                new Venue
                {
                    Name = "Kronotrop Coffee Bar Cihangir",
                    District = "Beyoğlu",
                    Neighborhood = "Cihangir",
                    Address = "Kuloğlu Mah. Turnacıbaşı Cad. No:53, Cihangir, Beyoğlu",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Nitelikli Zincir", "Cihangir Ruhu", "Hızlı Kahve", "Rahat Buluşma" },
                    SeatingArrangement = "Sokak önü yüksek tabureler ve loş iç köşe",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.6,
                    GoogleRating = 4.5,
                    ReviewCount = 2100,
                    HeroImageUrl = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Kronotrop+Cihangir",
                    InstagramHandle = "@kronotrop"
                },
                new Venue
                {
                    Name = "Beyaz Fırın & Brasserie Ataşehir",
                    District = "Ataşehir",
                    Neighborhood = "Barbaros / Watergarden",
                    Address = "Barbaros Mah. Şebboy Sok. No:2, Ataşehir",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Tatlı & Kruvasan", "Ferah Brasserie", "Klasik", "Ataşehir Buluşması" },
                    SeatingArrangement = "Cam tavanlı geniş kış bahçesi ve konforlu koltuklar",
                    HasAlcohol = false,
                    HasValetParking = true,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.8,
                    GoogleRating = 4.5,
                    ReviewCount = 3400,
                    HeroImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Beyaz+Fırın+Ataşehir",
                    InstagramHandle = "@beyazfirin"
                },

                // ==========================================
                // --- 3. SAHİL KENARI & DENİZ MANZARALI ---
                // ==========================================
                new Venue
                {
                    Name = "Aşşk Kahve Kuruçeşme",
                    District = "Beşiktaş",
                    Neighborhood = "Kuruçeşme",
                    Address = "Kuruçeşme Mah. Muallim Naci Cad. No:64/B, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Tek Ba��na Kafa Dinleme & Kahve" },
                    VibeTags = new() { "Boğaz Sıfır", "Romantik Bahçe", "Deniz Kokusu", "Tatlı & Kokteyl", "Sahil" },
                    SeatingArrangement = "Denize sıfır sarmaşıklı masalar ve loş fener aydınlatması",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.6,
                    GoogleRating = 4.5,
                    ReviewCount = 4500,
                    HeroImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Assk+Kahve+Kurucesme",
                    InstagramHandle = "@asskkahve"
                },
                new Venue
                {
                    Name = "Mangerie Bebek",
                    District = "Beşiktaş",
                    Neighborhood = "Bebek",
                    Address = "Bebek Mah. Cevdet Paşa Cad. No:69 Kat:3, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Y�ld�n�m� & �zel Ak�am" },
                    VibeTags = new() { "Panoramik Boğaz Terası", "Ferah Brunch", "Zarif & Şık", "Sahil Kenarı" },
                    SeatingArrangement = "Teras üzeri 2 kişilik ahşap masalar ve açık hava barı",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.5,
                    GoogleRating = 4.6,
                    ReviewCount = 2900,
                    HeroImageUrl = "https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Mangerie+Bebek",
                    InstagramHandle = "@mangeriebebek"
                },
                new Venue
                {
                    Name = "Tarihi Çınaraltı Çay Bahçesi",
                    District = "Üsküdar",
                    Neighborhood = "Çengelköy",
                    Address = "Çengelköy Mah. Çınaraltı Camii Sok. No:4, Üsküdar",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Asırlık Çınar Ağacı", "Denize Sıfır", "Nostaljik", "Samimi Sohbet", "Sahil Kenarı" },
                    SeatingArrangement = "Deniz kıyısında ahşap nostaljik sandalyeler",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.9,
                    GoogleRating = 4.6,
                    ReviewCount = 8200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Cinaralti+Cengelkoy",
                    InstagramHandle = "@cengelkoycinaralti"
                },
                new Venue
                {
                    Name = "Sunset Grill & Bar Ulus",
                    District = "Beşiktaş",
                    Neighborhood = "Ulus / Kuruçeşme",
                    Address = "Ulus Parkı İçi, Kuruçeşme Cad. No:2, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Kusursuz Boğaz Manzarası", "Fine Dining", "Lüks Romantik", "İmza Kokteyller" },
                    SeatingArrangement = "Panoramik Boğaz manzaralı teras ve mum ışıklı masalar",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.8,
                    GoogleRating = 4.7,
                    ReviewCount = 3700,
                    HeroImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Sunset+Grill+Bar+Ulus",
                    InstagramHandle = "@sunsetgrillbar"
                },
                new Venue
                {
                    Name = "Moda Tarihi İskele Cafe",
                    District = "Kadıköy",
                    Neighborhood = "Moda",
                    Address = "Caferağa Mah. Moda Cad. Tarihi İskele, Kadıköy",
                    PriceLevel = PriceLevel.Budget,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "�lk Bulu�ma & Romantik", "�� Toplant�s� & Freelance �al��ma" },
                    VibeTags = new() { "Tarihi Art Nouveau İskele", "Deniz Ortası", "Adalar Manzarası", "Gün Batımı" },
                    SeatingArrangement = "Deniz üstü açık teras ve tarihi taş salon masaları",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.2,
                    GoogleRating = 4.6,
                    ReviewCount = 4900,
                    HeroImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Moda+Tarihi+İskele",
                    InstagramHandle = "@modaiskelesi"
                },

                // ==========================================
                // --- 4. KOKTEYL BAR & PUB ---
                // ==========================================
                new Venue
                {
                    Name = "Fahri Konsolos Kokteyl & Bar",
                    District = "Kadıköy",
                    Neighborhood = "Moda",
                    Address = "Caferağa Mah. Dr. Esat Işık Cad. No:32/B, Kadıköy",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Ödüllü Kokteyller", "Loş & Karizmatik", "Samimi Bar", "Kokteyl Bar", "Yerel Otlar" },
                    SeatingArrangement = "Bar kenarı tabureler ve özel 2 kişilik köşeler",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = false,
                    FirstDateSuitabilityScore = 9.4,
                    GoogleRating = 4.7,
                    ReviewCount = 890,
                    HeroImageUrl = "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Fahri+Konsolos+Kadıköy",
                    InstagramHandle = "@fahrikonsolos"
                },
                new Venue
                {
                    Name = "Alexandra Cocktail Bar",
                    District = "Beşiktaş",
                    Neighborhood = "Arnavutköy",
                    Address = "Arnavutköy Mah. Bebek Cad. No:50, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.LivelyLoud,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Boğaz Terası", "İmza Kokteyller", "Şık & Havalı", "Kokteyl Bar", "Gece Enerjisi" },
                    SeatingArrangement = "Boğaz manzaralı teras ve loş kadife koltuklar",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.3,
                    GoogleRating = 4.5,
                    ReviewCount = 1800,
                    HeroImageUrl = "https://images.unsplash.com/photo-1572116469696-31de0f17cc34?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Alexandra+Cocktail+Bar+Arnavutköy",
                    InstagramHandle = "@alexandracocktailbar"
                },
                new Venue
                {
                    Name = "Geyik Coffee Roastery & Cocktail Bar",
                    District = "Beyoğlu",
                    Neighborhood = "Cihangir",
                    Address = "Kılıçali Paşa Mah. Akarsu Ykş. No:22/A, Cihangir, Beyoğlu",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Arkada�larla Muhabbet & E�lence", "Tek Ba��na Kafa Dinleme & Kahve" },
                    VibeTags = new() { "Bohem Cihangir", "Artizan Kokteyller", "Rahat Enerji", "Kokteyl Bar", "Pub" },
                    SeatingArrangement = "Sokağa bakan küçük tatlı masalar ve dinamik bar tezgahı",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.1,
                    GoogleRating = 4.5,
                    ReviewCount = 2100,
                    HeroImageUrl = "https://images.unsplash.com/photo-1543007630-9710e4a00a20?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Geyik+Cihangir",
                    InstagramHandle = "@geyikcihangir"
                },
                new Venue
                {
                    Name = "Lucca Style Bebek",
                    District = "Beşiktaş",
                    Neighborhood = "Bebek",
                    Address = "Bebek Mah. Cevdet Paşa Cad. No:51, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.LivelyLoud,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Bebek İkonu", "Trendsetter", "Özel Miksoloji", "Prestijli & Canlı" },
                    SeatingArrangement = "Merkezi şık bar etrafı ve yüksek bistro masalar",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.2,
                    GoogleRating = 4.4,
                    ReviewCount = 3100,
                    HeroImageUrl = "https://images.unsplash.com/photo-1514362545857-3bc16c4c7d1b?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Lucca+Bebek",
                    InstagramHandle = "@lucca_style"
                },
                new Venue
                {
                    Name = "Zeplin Pub & Delicatessen Moda",
                    District = "Kadıköy",
                    Neighborhood = "Moda",
                    Address = "Caferağa Mah. Moda Cad. No:70, Kadıköy",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Arkada�larla Muhabbet & E�lence", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "İngiliz Pub Ambiyansı", "Zengin Bira Menüsü", "Ahşap Sıcaklık", "Pub" },
                    SeatingArrangement = "Koyu ahşap kabinler ve sokak önü yüksek masalar",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.8,
                    GoogleRating = 4.5,
                    ReviewCount = 2700,
                    HeroImageUrl = "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Zeplin+Pub+Moda",
                    InstagramHandle = "@zeplinpub"
                },
                new Venue
                {
                    Name = "The Populist Bomontiada",
                    District = "Şişli",
                    Neighborhood = "Bomonti",
                    Address = "Merkez Mah. Birahane Sok. Bomontiada No:1, Şişli",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.LivelyLoud,
                    SuitableOccasions = new() { "Arkada�larla Muhabbet & E�lence", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Craft Bira", "Bomontiada Avlusu", "Dinamik Müzik", "Pub", "Eğlenceli" },
                    SeatingArrangement = "Geniş fabrika mimarisi masalar ve açık avlu bistro",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.9,
                    GoogleRating = 4.6,
                    ReviewCount = 4800,
                    HeroImageUrl = "https://images.unsplash.com/photo-1514933651103-005eec06c04b?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=The+Populist+Bomontiada",
                    InstagramHandle = "@thepopulisttr"
                },

                // ==========================================
                // --- 5. ŞARAP EVİ & ROMANTİK MAHZEN ---
                // ==========================================
                new Venue
                {
                    Name = "Viktor Levi Şarap Evi",
                    District = "Kadıköy",
                    Neighborhood = "Moda / Caferağa",
                    Address = "Caferağa Mah. Moda Cad. Damacı Sok. No:4, Kadıköy",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Y�ld�n�m� & �zel Ak�am" },
                    VibeTags = new() { "Tarihi Şarap Mahzeni", "Sarmaşıklı Gizli Bahçe", "Mum Işığı", "Romantik", "Şarap Evi" },
                    SeatingArrangement = "Sarmaşıklar altında ahşap masalar ve loş iç mahzen",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.8,
                    GoogleRating = 4.6,
                    ReviewCount = 3200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Viktor+Levi+Kadıköy",
                    InstagramHandle = "@viktorlevisarap"
                },
                new Venue
                {
                    Name = "Pano Şarap Evi 1898",
                    District = "Beyoğlu",
                    Neighborhood = "Galatasaray / Asmalımescit",
                    Address = "Hüseyinağa Mah. Hamalbaşı Cad. No:16, Beyoğlu",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Y�ld�n�m� & �zel Ak�am" },
                    VibeTags = new() { "1898 Tarihi Miras", "Vitray Camlar", "Peynir Tabakları", "Romantik Mahzen", "Şarap Evi" },
                    SeatingArrangement = "Tarihi vitraylı ahşap localar ve mum ışıklı masalar",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = true,
                    HasOutdoorSeating = false,
                    FirstDateSuitabilityScore = 9.5,
                    GoogleRating = 4.5,
                    ReviewCount = 2600,
                    HeroImageUrl = "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Pano+Sarap+Evi",
                    InstagramHandle = "@panosarapevi"
                },
                new Venue
                {
                    Name = "Solera Winery",
                    District = "Beyoğlu",
                    Neighborhood = "Galatasaray / Yeniçarşı",
                    Address = "Tomtom Mah. Yeni Çarşı Cad. No:397, Beyoğlu",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Y�ld�n�m� & �zel Ak�am" },
                    VibeTags = new() { "Butik Şarap Mahzeni", "Samimi Loşluk", "Tadım Menüsü", "Şarap Evi" },
                    SeatingArrangement = "Şarap şişeleriyle çevrili dar ve çok samimi ahşap masalar",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.4,
                    GoogleRating = 4.7,
                    ReviewCount = 1950,
                    HeroImageUrl = "https://images.unsplash.com/photo-1506377247377-2a5b3b417ebb?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Solera+Winery+Beyoglu",
                    InstagramHandle = "@solerawinery"
                },
                new Venue
                {
                    Name = "Sensus Şarap Galerisi",
                    District = "Beyoğlu",
                    Neighborhood = "Galata / Kuledibi",
                    Address = "Bereketzade Mah. Büyükhendek Cad. No:5, Galata, Beyoğlu",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Y�ld�n�m� & �zel Ak�am" },
                    VibeTags = new() { "Galata Kulesi Altı", "Tarihi Taş Mahzen", "Yerel Üretim Şaraplar", "Şarap Evi" },
                    SeatingArrangement = "Kemerli taş duvarlar altında mum ışığı ve yumuşak ışıklandırma",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = true,
                    HasOutdoorSeating = false,
                    FirstDateSuitabilityScore = 9.3,
                    GoogleRating = 4.6,
                    ReviewCount = 2200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Sensus+Galata",
                    InstagramHandle = "@sensussarap"
                },

                // ==========================================
                // --- 6. ŞIK RESTORAN & AKŞAM YEMEĞİ ---
                // ==========================================
                new Venue
                {
                    Name = "Cecconi's Istanbul",
                    District = "Beyoğlu",
                    Neighborhood = "Pera / Asmalımescit",
                    Address = "Evliya Çelebi Mah. Meşrutiyet Cad. No:56, Soho House Bahçesi, Beyoğlu",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Zeytin Ağaçları Bahçesi", "Modern İtalyan", "Mum Işığı", "Prestijli", "Şık Restoran" },
                    SeatingArrangement = "Bahçe avlusu içinde geniş yuvarlak ve ikili şık masalar",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.8,
                    GoogleRating = 4.6,
                    ReviewCount = 3500,
                    HeroImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Cecconis+Istanbul",
                    InstagramHandle = "@cecconisistanbul"
                },
                new Venue
                {
                    Name = "Apartiman Yeniköy",
                    District = "Sarıyer",
                    Neighborhood = "Yeniköy",
                    Address = "Yeniköy Mah. Köybaşı Cad. No:153, Sarıyer",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Tarladan Masaya", "Ev Sıcaklığı", "Butik Bahçe", "Zarif & Sade", "Şık Restoran" },
                    SeatingArrangement = "Sessiz arka bahçe ve samimi ahşap masalar",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.7,
                    GoogleRating = 4.7,
                    ReviewCount = 1200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Apartiman+Yeniköy",
                    InstagramHandle = "@apartimanyenikoy"
                },
                new Venue
                {
                    Name = "Glens Nişantaşı",
                    District = "Şişli",
                    Neighborhood = "Nişantaşı / Teşvikiye",
                    Address = "Harbiye Mah. Abdi İpekçi Cad. No:12, Nişantaşı, Şişli",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Moda Caddesi", "Şık Brasserie", "Akdeniz Mutfağı", "Etkileyici", "Şık Restoran" },
                    SeatingArrangement = "Loş ışıklı lüks oturma grupları ve cam tavanlı kış bahçesi",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.4,
                    GoogleRating = 4.4,
                    ReviewCount = 1600,
                    HeroImageUrl = "https://images.unsplash.com/photo-1550966871-3ed3cdb5ed0c?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Glens+Nişantaşı",
                    InstagramHandle = "@glensistanbul"
                },
                new Venue
                {
                    Name = "Aheste Pera",
                    District = "Beyoğlu",
                    Neighborhood = "Meşrutiyet / Asmalımescit",
                    Address = "Asmalı Mescit Mah. Meşrutiyet Cad. No:107/F, Beyoğlu",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Y�ld�n�m� & �zel Ak�am" },
                    VibeTags = new() { "Modern Meyhane & Meze", "Tarihi Sarnıç Havası", "Loş Mum Işığı", "Şık Restoran" },
                    SeatingArrangement = "Tuğla kemerler altında 2 kişilik samimi masalar",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = true,
                    HasOutdoorSeating = false,
                    FirstDateSuitabilityScore = 9.6,
                    GoogleRating = 4.7,
                    ReviewCount = 2100,
                    HeroImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Aheste+Pera",
                    InstagramHandle = "@ahestepera"
                },
                new Venue
                {
                    Name = "Alaf Kuruçeşme",
                    District = "Beşiktaş",
                    Neighborhood = "Kuruçeşme",
                    Address = "Kuruçeşme Mah. Kuruçeşme Cad. No:19, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "Göçebe Mutfağı", "Boğaz Terası", "Odun Ateşi", "Gurme Deneyim", "Şık Restoran" },
                    SeatingArrangement = "Teras Boğaz manzarası ve açık mutfak şef masası",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.5,
                    GoogleRating = 4.6,
                    ReviewCount = 1400,
                    HeroImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Alaf+Kurucesme",
                    InstagramHandle = "@alafkurucesme"
                },
                new Venue
                {
                    Name = "Da Mario Ristorante Etiler",
                    District = "Beşiktaş",
                    Neighborhood = "Etiler",
                    Address = "Etiler Mah. Dilhayat Sok. No:7, Beşiktaş",
                    PriceLevel = PriceLevel.Premium,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Kutlama, Do�um G�n� & Grup" },
                    VibeTags = new() { "İtalyan Klasiği", "Zarif Bahçe", "Odun Fırını Pizza & Şarap", "Şık Restoran" },
                    SeatingArrangement = "Yeşillikler içinde geniş aralıklı bahçe masaları",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.4,
                    GoogleRating = 4.5,
                    ReviewCount = 2800,
                    HeroImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Da+Mario+Etiler",
                    InstagramHandle = "@damarioist"
                },

                // ==========================================
                // --- 7. ÇEVRE & DİĞER POPÜLER İLÇELER ---
                // ==========================================
                new Venue
                {
                    Name = "Midpoint Aqua Florya",
                    District = "Bakırköy",
                    Neighborhood = "Florya / Şenlikköy",
                    Address = "Şenlikköy Mah. Yeşilköy Halkalı Cad. Aqua Florya AVM No:93, Bakırköy",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.ModerateMusic,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "Arkada�larla Muhabbet & E�lence" },
                    VibeTags = new() { "Panoramik Marmara Denizi", "Geniş Teras", "Akşam Gün Batımı", "Sahil Kenarı" },
                    SeatingArrangement = "Denize sıfır teras loca koltukları ve camlı iç mekan",
                    HasAlcohol = true,
                    HasValetParking = true,
                    RequiresReservation = false,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 8.8,
                    GoogleRating = 4.4,
                    ReviewCount = 5200,
                    HeroImageUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Midpoint+Aqua+Florya",
                    InstagramHandle = "@midpointtr"
                },
                new Venue
                {
                    Name = "Forno Balat",
                    District = "Fatih",
                    Neighborhood = "Balat",
                    Address = "Balat Mah. Fener Kireçhane Sok. No:13/A, Fatih",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "Tek Ba��na Kafa Dinleme & Kahve", "�lk Bulu�ma & Romantik" },
                    VibeTags = new() { "Taş Fırın Lahmacun & Pizza", "Balat Nostaljisi", "Butik Sıcaklık", "Samimi" },
                    SeatingArrangement = "Açık fırın manzaralı ahşap küçük masalar ve sokak önü",
                    HasAlcohol = false,
                    HasValetParking = false,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.1,
                    GoogleRating = 4.6,
                    ReviewCount = 2900,
                    HeroImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Forno+Balat",
                    InstagramHandle = "@fornobalat"
                },
                new Venue
                {
                    Name = "Splendid Palace Hotel Cafe & Bahçe",
                    District = "Adalar",
                    Neighborhood = "Büyükada",
                    Address = "Büyükada Maden Mah. 23 Nisan Cad. No:53, Adalar",
                    PriceLevel = PriceLevel.Moderate,
                    NoiseLevel = NoiseLevel.WhisperQuiet,
                    SuitableOccasions = new() { "�lk Bulu�ma & Romantik", "Tek Ba��na Kafa Dinleme & Kahve" },
                    VibeTags = new() { "1908 Tarihi Otel Avlusu", "Ada Nostaljisi", "Kırmızı Panjurlar", "Romantik" },
                    SeatingArrangement = "Tarihi avluda havuz kenarı ferah bahçe masaları",
                    HasAlcohol = true,
                    HasValetParking = false,
                    RequiresReservation = true,
                    HasOutdoorSeating = true,
                    FirstDateSuitabilityScore = 9.6,
                    GoogleRating = 4.7,
                    ReviewCount = 2400,
                    HeroImageUrl = "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3?q=80&w=1200",
                    GoogleMapsUrl = "https://maps.google.com/?q=Splendid+Palas+Buyukada",
                    InstagramHandle = "@splendidpalacebuyukada"
                }
            };
    }

    public static List<Venue> GetStaticSeedVenues(string? district, List<string>? coveredDistricts)
    {
        var all = GetAllInitialVenues();
        var matched = new List<Venue>();

        if (coveredDistricts != null && coveredDistricts.Any())
        {
            var lowerList = coveredDistricts.Select(d => d.ToLower().Trim()).ToList();
            matched = all.Where(v => lowerList.Contains(v.District.ToLower().Trim())).ToList();
        }
        else if (!string.IsNullOrWhiteSpace(district) && 
            !district.Equals("Tüm İstanbul", StringComparison.OrdinalIgnoreCase) && 
            !district.Equals("Hepsi", StringComparison.OrdinalIgnoreCase))
        {
            matched = all.Where(v => v.District.Equals(district.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Eğer eşleşen mekan sayısı 3'ten az ise (örneğin tek bir mekan varsa veya hiç yoksa),
        // kullanıcının her zaman en az 3 mekan görebilmesi için havuzdaki diğer en iyi mekanlarla listeyi tamamla
        if (matched.Count < 3)
        {
            var matchedNames = matched.Select(m => m.Name.ToLower()).ToHashSet();
            var additional = all
                .Where(v => !matchedNames.Contains(v.Name.ToLower()))
                .OrderByDescending(v => v.FirstDateSuitabilityScore)
                .ToList();

            matched.AddRange(additional);
        }

        return matched;
    }
}

