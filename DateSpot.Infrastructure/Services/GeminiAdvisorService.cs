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
                results[venue.Id] = GenerateFallbackAdvice(venue, requestedConcept);
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
Sen uzman bir şehir rehberi ve mekan küratörüsün. Kullanıcı İstanbul ({district}) bölgesinde '{occasionText}' amacı ve '{conceptName}' tarzında bir mekan arıyor.
Aşağıda seçilen 3 mekan için her biri adına Türkçe olarak şu 4 alanı doldur:
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
                results[venue.Id] = GenerateFallbackAdvice(venue, requestedConcept);
            }
        }

        return results;
    }

    private static DateSpotAdviceResult GenerateFallbackAdvice(Venue venue, DateConcept concept)
    {
        return concept switch
        {
            DateConcept.QuietAndIntimate => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, masalar arası mesafesi ve loş ışıklarıyla dış dünyadan izole, derin ve göz teması yüksek bir ilk buluşma sunar.",
                IcebreakerTopic = "Mekanın mimarisi veya fonda çalan hafif müzikten yola çıkarak son zamanlarda keşfettiğiniz sakin köşeleri sorabilirsiniz.",
                TableTactics = "Karşılıklı oturmak yerine L şeklinde bitişik köşe masaları tercih edin, bu beden dilindeki savunma bariyerini kırar.",
                IdealOrderRecommendation = "Paylaşımlı bir filtre kahve veya aromatik bitki çayı + hafif bir tart."
            },
            DateConcept.RomanticAndChic => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, özenle tasarlanmış şık ambiyansı ve romantik ışıklandırmasıyla karşı tarafa 'özenilmiş' bir intiba bırakmak için kusursuzdur.",
                IcebreakerTopic = "İmza kokteyllerin veya tatlıların hikayesini garsona sorarak sohbete eğlenceli bir ortak merak katın.",
                TableTactics = "Giriş kapısından uzak, duvar kenarındaki konforlu oturma alanını rica edin.",
                IdealOrderRecommendation = "İmza kokteyl veya kaliteli bir kadeh şarap eşliğinde paylaşımlı peynir/şarküteri tabağı."
            },
            DateConcept.CocktailAndVibe => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, dinamik bar arkası ekibi ve seçkin kokteyl menüsüyle ilk buluşmanın sessizlik anlarını harika bir enerjiyle doldurur.",
                IcebreakerTopic = "'Klasik tatlar insanı mısın yoksa ekşi/baharatlı deneysel şeyleri sever misin?' sorusuyla tat profili analizi yapın.",
                TableTactics = "Bar taburelerinde yan yana oturmak ilk teması ve rahatlığı en çok artıran pozisyondur.",
                IdealOrderRecommendation = "Barmen tavsiyesi imza kokteyller ve finger food atıştırmalıklar."
            },
            DateConcept.CoffeeAndWalk => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, samimi üçüncü nesil kahve kültürü ve ferah oturma alanıyla 'sıfır baskı' hissettiren tatlı bir ilk tanışma mekanıdır.",
                IcebreakerTopic = "'Pazar sabahı kahve ritüelin nedir?' gibi hafif ve gündelik keyifler üzerine konuşun.",
                TableTactics = "İç mekan yerine yarı açık bahçe kısmında oturup sonrasında kısa bir yürüyüşe geçiş kapısı bırakın.",
                IdealOrderRecommendation = "Flat White veya soğuk demleme kahve + meşhur San Sebastian cheesecake."
            },
            _ => new DateSpotAdviceResult
            {
                WhyThisSpot = $"{venue.Name}, kaliteli hizmeti ve rahatlatıcı enerjisiyle ilk buluşma için tam aradığınız dengeli atmosferi sağlar.",
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
