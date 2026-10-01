using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DateSpot.Core.Interfaces;

namespace DateSpot.Infrastructure.Services;

public class RevenueCatService : IRevenueCatService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RevenueCatService> _logger;

    public RevenueCatService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RevenueCatService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsSubscriptionActiveAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appUserId))
        {
            return false;
        }

        string apiKey = _configuration["RevenueCat:SecretApiKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_REVENUECAT_SECRET_KEY")
        {
            _logger.LogInformation("RevenueCat API Key yapılandırılmamış. Test/Geliştirme modunda erişim aktif kabul ediliyor.");
            return true; // Geliştirme ortamında testleri engellememek için
        }

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.revenuecat.com/v1/subscribers/{appUserId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("X-Platform", "stripe");

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(content);

                // 'entitlements' içerisindeki 'premium_access' veya aylık abonelik yetkisini kontrol et
                if (doc.RootElement.TryGetProperty("subscriber", out var subscriber) &&
                    subscriber.TryGetProperty("entitlements", out var entitlements))
                {
                    foreach (var prop in entitlements.EnumerateObject())
                    {
                        if (prop.Value.TryGetProperty("expires_date", out var expiresDateProp))
                        {
                            if (DateTime.TryParse(expiresDateProp.GetString(), out var expiresDate))
                            {
                                if (expiresDate > DateTime.UtcNow)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RevenueCat abonelik doğrulaması yapılırken hata oluştu.");
        }

        return false;
    }
}
