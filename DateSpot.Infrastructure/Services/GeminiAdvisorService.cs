using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DateSpot.Core.Entities;
using DateSpot.Core.Enums;
using DateSpot.Core.Interfaces;

namespace DateSpot.Infrastructure.Services;

public class GeminiAdvisorService : IGeminiAdvisorService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAdvisorService> _logger;

    public GeminiAdvisorService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAdvisorService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// LLM-as-a-Ranker & Curator:
    /// SQL aday havuzundan gelen mekanları kullanıcının derin niyetine göre analiz edip
    /// doğrudan Gemini zekasıyla 1'den 10'a sıralar ve kişiselleştirilmiş taktikleri üretir.
    /// </summary>
    public async Task<List<GeminiCuratedVenueItem>> RankAndCurateVenuesAsync(
        List<Venue> candidatePool,
        string occasion,
        DateConcept requestedConcept,
        string venueType,
        string groupSize,
        string dateTiming,
        int? noisePreference,
        string district,
        string customPrompt = "",
        int targetCount = 10,
        CancellationToken cancellationToken = default)
    {
        string apiKey = _configuration["Gemini:ApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
        {
            _logger.LogInformation("[GeminiAdvisorService] API Key tanımlı değil, dahili C# ağırlıklı sıralama devrede.");
            return new List<GeminiCuratedVenueItem>();
        }

        try
        {
            string occasionText = !string.IsNullOrWhiteSpace(occasion) ? occasion : "Özel Buluşma";
            string venueTypeText = !string.IsNullOrWhiteSpace(venueType) ? venueType : "Herhangi";
            string groupSizeText = !string.IsNullOrWhiteSpace(groupSize) ? groupSize : "1-2 Kişi";
            string timingText = !string.IsNullOrWhiteSpace(dateTiming) ? dateTiming : "Kahve & Tatlı";
            string noiseText = noisePreference switch
            {
                1 => "Sakin, sessiz ve fısıltılı konuşma",
                3 => "Canlı, enerjik, popüler ve trend",
                _ => "Dengeli, tatlı uğultulu ve ideal müzikli"
            };

            string customPromptSection = !string.IsNullOrWhiteSpace(customPrompt)
                ? $"\n- 🪄 KULLANICININ ÖZEL HAYALİNDEKİ MEKAN METNİ (Kendi İfadeleriyle): \"{customPrompt}\"\n(ÖNEMLİ: Bu özel metindeki tüm atmosfer, manzara, menü ve müzik detaylarını en yüksek öncelikle dikkate al!)"
                : "";

            var venuesSummary = candidatePool.Select(v => new
            {
                v.Id,
                v.Name,
                v.District,
                v.Neighborhood,
                Rating = v.GoogleRating,
                Reviews = v.ReviewCount,
                Price = v.PriceLevel.ToString(),
                Noise = v.NoiseLevel.ToString(),
                Lighting = v.LightingStyle,
                Spacing = v.TableSpacing,
                v.BestTableTip,
                v.SeatingArrangement,
                HasOutdoor = v.HasOutdoorSeating,
                HasAlcohol = v.HasAlcohol,
                HasWifi = v.HasWifiAndSockets,
                Vibes = string.Join(", ", v.VibeTags),
                Cuisines = string.Join(", ", v.CuisineTypes),
                Signatures = string.Join(", ", v.SignatureItems),
                SuitableOccasions = string.Join(", ", v.SuitableOccasions)
            }).ToList();

            string prompt = $@"
Sen dünyanın en prestijli şehir rehberi, mekan küratörü ve deneyim danışmanısın.
Kullanıcı İstanbul ({district}) bölgesinde şu niyetle bir mekan arıyor:
- Buluşma Amacı / Occasion: {occasionText}
- Mekan Tarzı / Venue Type: {venueTypeText}
- Masadaki Kişi Sayısı: {groupSizeText}
- Öğün / Format: {timingText}
- Ortam Ses Tercihi: {noiseText}{customPromptSection}

GÖREVİN:
Aşağıdaki aday mekan havuzunu ({venuesSummary.Count} mekan) derinlemesine incele.
Kullanıcının niyetine ve ambiyansına EN MÜKEMMEL uyan {targetCount} mekanı SEÇ ve en yüksek uyumdan en düşüğe doğru 1'den {targetCount}'a sırala.

Her mekan için şu alanları üret:
1. id: Mekanın GUID'i (tam eşleşmeli).
2. matchScore: 80 - 99 arası genel uyum yüzdesi (1. sıradaki 96-99, 10. sıradaki 82-88).
3. occasionScore: Buluşma amacına uyum (75 - 99).
4. vibeScore: Ambiyans, müzik ve ışık uyumu (75 - 99).
5. seatingScore: Masa, oturma ve mahremiyet uyumu (75 - 99).
6. budgetScore: Fiyat ve format uyumu (75 - 99).
7. whyThisSpot: Bu mekanın {occasionText} için neden en doğru seçim olduğunu anlatan 2 ikna edici cümle.
8. icebreakerTopic: Bu mekanda ve bu ortamda masada konuşulabilecek ortama özel zekice bir sohbet konusu / ortam tüyosu.
9. tableTactics: Hangi masaya oturulmalı veya ne rezerve edilmeli? (Örn: 'Boğaz gören sol köşe masa', 'Sessiz prizli arka masa', 'Mangala yakın ferah bahçe').
10. idealOrderRecommendation: Mekanın imza menüsünden en risksiz ve lezzetli 1-2 sipariş önerisi.

Aday Mekanlar:
{JsonSerializer.Serialize(venuesSummary)}

LÜTFEN SADECE ŞU JSON FORMATINDA CEVAP VER:
[
  {{
    ""id"": ""GUID"",
    ""matchScore"": 97.5,
    ""occasionScore"": 98.0,
    ""vibeScore"": 96.0,
    ""seatingScore"": 94.0,
    ""budgetScore"": 95.0,
    ""whyThisSpot"": ""..."",
    ""icebreakerTopic"": ""..."",
    ""tableTactics"": ""..."",
    ""idealOrderRecommendation"": ""...""
  }}
]";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.4,
                    responseMimeType = "application/json"
                }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
                var rawText = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (!string.IsNullOrWhiteSpace(rawText))
                {
                    var parsed = JsonSerializer.Deserialize<List<GeminiCuratedVenueItem>>(rawText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsed != null && parsed.Any())
                    {
                        return parsed;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GeminiAdvisorService] AI Ranker çağrısı başarısız oldu. Hibrit matematik motoruna dönülüyor.");
        }

        return new List<GeminiCuratedVenueItem>();
    }

    public async Task<Dictionary<Guid, DateSpotAdviceResult>> GenerateDateAdvicesAsync(
        List<Venue> topVenues,
        DateConcept requestedConcept,
        string district,
        string occasion = "",
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<Guid, DateSpotAdviceResult>();
        string apiKey = _configuration["Gemini:ApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY")
        {
            _logger.LogInformation("Gemini API Key bulunamadı veya varsayılan değerde. Akıllı dahili şablon motoru kullanılıyor.");
            foreach (var venue in topVenues)
            {
                results[venue.Id] = GenerateFallbackAdvice(venue, requestedConcept, occasion);
            }
            return results;
        }

        try
        {
            string occasionText = !string.IsNullOrWhiteSpace(occasion) 
                ? occasion 
                : "Özel Buluşma & Mekan Keşfi";

            string conceptName = requestedConcept switch
            {
                DateConcept.QuietAndIntimate => "Sessiz, Samimi ve Rahat Ortam",
                DateConcept.RomanticAndChic => "Romantik, Loş Işık ve Şık",
                DateConcept.CocktailAndVibe => "Kaliteli Kokteyl ve Sosyal Atmosfer",
                DateConcept.CoffeeAndWalk => "Kahve, Tatlı ve Rahat Oturum",
                DateConcept.FunAndCasual => "Eğlenceli, Rahat ve Dinamik",
                _ => "Mekan Buluşması"
            };

            var venuesSummary = topVenues.Select(v => new
            {
                v.Id,
                v.Name,
                v.District,
                v.Neighborhood,
                Vibes = string.Join(", ", v.VibeTags),
                Cuisines = string.Join(", ", v.CuisineTypes),
                SignatureDishes = string.Join(", ", v.SignatureItems),
                Views = string.Join(", ", v.ViewType),
                v.LightingStyle,
                v.TableSpacing,
                v.BestTableTip,
                v.SeatingArrangement,
                Noise = v.NoiseLevel.ToString(),
                HasAlcohol = v.HasAlcohol
            });

            string prompt = $@"
Sen uzman bir şehir rehberi ve mekan küratörüsün. Kullanıcı İstanbul ({district}) bölgesinde '{occasionText}' amacı ve '{conceptName}' tarzında mekanlar arıyor.
Aşağıda listelenen en uygun mekanlar için her biri adına Türkçe olarak şu 4 alanı doldur:
1. WhyThisSpot: Bu mekanın '{occasionText}' için neden en mükemmel tercih olduğunu anlatan etkileyici ve ikna edici 2-3 cümle.
2. IcebreakerTopic: Bu mekanın ambiyansına ve '{occasionText}' amacına uygun masada açılabilecek zekice/keyifli bir sohbet konusu veya ortam tüyosu.
3. TableTactics: Masada oturma düzeni, rezerve edilecek en iyi köşe veya ortam taktiği (Örn: 'Bahçe tarafındaki köşeyi isteyin', 'Toplantı/sohbet için arka sessiz masayı seçin').
4. IdealOrderRecommendation: Bu mekanda sipariş edilmesi en tavsiye edilen, risksiz ve popüler yiyecek/içecek önerisi (Mekanın SignatureDishes verilerinden ilham al).

Mekanlar:
{JsonSerializer.Serialize(venuesSummary)}

Lütfen sonucu SADECE şu JSON formatında döndür:
[
  {{
    ""id"": ""GUID"",
    ""whyThisSpot"": ""..."",
    ""icebreakerTopic"": ""..."",
    ""tableTactics"": ""..."",
    ""idealOrderRecommendation"": ""...""
  }}
]";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    responseMimeType = "application/json"
                }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(cancellationToken: cancellationToken);
                var rawText = responseJson?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (!string.IsNullOrWhiteSpace(rawText))
                {
                    var parsed = JsonSerializer.Deserialize<List<GeminiAdviceItem>>(rawText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (parsed != null)
                    {
                        foreach (var item in parsed)
                        {
                            if (Guid.TryParse(item.Id, out var vId))
                            {
                                results[vId] = new DateSpotAdviceResult
                                {
                                    WhyThisSpot = item.WhyThisSpot,
                                    IcebreakerTopic = item.IcebreakerTopic,
                                    TableTactics = item.TableTactics,
                                    IdealOrderRecommendation = item.IdealOrderRecommendation
                                };
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API çağrısı sırasında hata oluştu. Dahili tavsiye motoruna geçiliyor.");
        }

        // Eğer Gemini'den eksik kalan mekan varsa fallback ile tamamla
        foreach (var venue in topVenues)
        {
            if (!results.ContainsKey(venue.Id))
            {
                results[venue.Id] = GenerateFallbackAdvice(venue, requestedConcept, occasion);
            }
        }

        return results;
    }

    public static DateSpotAdviceResult GenerateFallbackAdvice(Venue venue, DateConcept concept, string occasion = "")
    {
        var occ = (occasion ?? string.Empty).ToLower();

        if (occ.Contains("mangal") || occ.Contains("et") || occ.Contains("ocakbaşı"))
        {
            return new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, ferah açık alanı ve et/ızgara lezzetleriyle arkadaş grubunuzla keyifli ve dumanaltı olmadan buluşabileceğiniz harika bir noktadır.",
                IcebreakerTopic = "Masanın en iyi ızgara ustasının kim olduğu veya geçmiş unutulmaz arkadaşlık anıları üzerine sohbet başlatabilirsiniz.",
                TableTactics = "Hava sirkülasyonunun iyi olduğu, servise yakın geniş bahçe veya köşe masayı tercih edin.",
                IdealOrderRecommendation = (venue.SignatureItems != null && venue.SignatureItems.Any()) 
                    ? string.Join(", ", venue.SignatureItems.Take(2)) 
                    : "Karışık ızgara tabağı ve paylaşımlı mezeler."
            };
        }

        if (occ.Contains("kutlama") || occ.Contains("doğum"))
        {
            return new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, canlı ve neşeli atmosferiyle doğum günü ve özel kutlamalarınızda grubunuzu en iyi şekilde ağırlayacak enerjiye sahiptir.",
                IcebreakerTopic = "Günün anlam ve önemine dair keyifli anıları tazeleyip kadehleri kaldırın.",
                TableTactics = "Pasta kesimi ve grup fotoğrafları için arka planı şık, masa birleşimine müsait ana salon köşesini ayırtın.",
                IdealOrderRecommendation = "Paylaşımlı kutlama mezeleri, imza içecekler ve özel tatlı."
            };
        }

        if (occ.Contains("iş") || occ.Contains("çalışma") || occ.Contains("freelance"))
        {
            return new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, kurumsal sessizliği, ferah masaları ve hızlı bağlantısıyla verimli toplantılar veya odaklanmış çalışma için idealdir.",
                IcebreakerTopic = "Sektörel trendler veya üzerinde çalıştığınız güncel projeler üzerinden profesyonel bir giriş yapın.",
                TableTactics = "Priz erişimi olan, gürültüden ve ana geçiş yolundan uzak izole bir masa seçin.",
                IdealOrderRecommendation = "Büyük boy V60 filtre kahve veya soğuk demleme + hafif bir kruvasan."
            };
        }

        if (occ.Contains("arkadaş") || occ.Contains("eğlence") || occ.Contains("muhabbet"))
        {
            return new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, samimi ve akıcı enerjisiyle dostlarınızla uzun uzun sohbet edip stres atabileceğiniz dinamik bir ortam sunar.",
                IcebreakerTopic = "Son dönemde dinlediğiniz yeni müzikler, seyahat planları veya komik gündelik olaylar.",
                TableTactics = "Herkesin birbirini rahatça görebileceği yuvarlak veya L oturma düzenini tercih edin.",
                IdealOrderRecommendation = "Paylaşımlı sıcak atıştırmalık sepeti ve favori içecekler."
            };
        }

        if (occ.Contains("piknik") || occ.Contains("doğa") || occ.Contains("açık hava"))
        {
            return new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, şehrin gürültüsünden uzak yemyeşil doğası ve manzarasıyla tam anlamıyla huzur depolama noktasıdır.",
                IcebreakerTopic = "Doğanın dinginliğinde gelecek planları, kitaplar veya doğa yürüyüşü rotaları üzerine konuşun.",
                TableTactics = "Manzarayı panoramik gören, gölge altındaki seyir veya çimlik bölgeyi seçin.",
                IdealOrderRecommendation = "Termosta demli çay/kahve eşliğinde taze börek veya sandviç."
            };
        }

        return concept switch
        {
            DateConcept.QuietAndIntimate => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, masalar arası mesafesi ve loş ışıklarıyla dış dünyadan izole, derin ve göz teması yüksek bir buluşma sunar.",
                IcebreakerTopic = "Mekanın mimarisi veya fonda çalan hafif müzikten yola çıkarak son zamanlarda keşfettiğiniz sakin köşeleri sorabilirsiniz.",
                TableTactics = "Karşılıklı oturmak yerine L şeklinde bitişik köşe masaları tercih edin, bu savunma bariyerini kırar.",
                IdealOrderRecommendation = "Paylaşımlı bir filtre kahve veya aromatik bitki çayı + hafif bir tart."
            },
            DateConcept.RomanticAndChic => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, özenle tasarlanmış şık ambiyansı ve romantik ışıklandırmasıyla karşı tarafa 'özenilmiş' bir intiba bırakmak için kusursuzdur.",
                IcebreakerTopic = "İmza kokteyllerin veya tatlıların hikayesini garsona sorarak sohbete eğlenceli bir ortak merak katın.",
                TableTactics = "Giriş kapısından uzak, duvar kenarındaki konforlu oturma alanını rica edin.",
                IdealOrderRecommendation = "İmza kokteyl veya kaliteli bir kadeh şarap eşliğinde paylaşımlı peynir tabağı."
            },
            DateConcept.CocktailAndVibe => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, dinamik bar arkası ekibi ve seçkin menüsüyle buluşmanın enerjisini en üst seviyede tutar.",
                IcebreakerTopic = "'Klasik tatlar insanı mısın yoksa ekşi/baharatlı deneysel şeyleri sever misin?' sorusuyla tat profili analizi yapın.",
                TableTactics = "Bar taburelerinde yan yana oturmak ilk teması ve rahatlığı en çok artıran pozisyondur.",
                IdealOrderRecommendation = "Barmen tavsiyesi imza kokteyller ve finger food atıştırmalıklar."
            },
            _ => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, kaliteli hizmeti ve rahatlatıcı enerjisiyle buluşmanız için tam aradığınız dengeli atmosferi sağlar.",
                IcebreakerTopic = "Mekanın lokasyonu ve semt kültürüne dair anılarınızı paylaşın.",
                TableTactics = "Ortamın en aydınlık değil, sıcak sarı ışık alan köşesini seçin.",
                IdealOrderRecommendation = "Mekanın en çok övülen imza içeceği ve paylaşımlı tatlısı."
            }
        };
    }
}

public class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class GeminiAdviceItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("whyThisSpot")]
    public string WhyThisSpot { get; set; } = string.Empty;

    [JsonPropertyName("icebreakerTopic")]
    public string IcebreakerTopic { get; set; } = string.Empty;

    [JsonPropertyName("tableTactics")]
    public string TableTactics { get; set; } = string.Empty;

    [JsonPropertyName("idealOrderRecommendation")]
    public string IdealOrderRecommendation { get; set; } = string.Empty;
}
