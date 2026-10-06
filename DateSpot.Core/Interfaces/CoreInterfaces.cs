using DateSpot.Core.Entities;
using DateSpot.Core.Enums;

namespace DateSpot.Core.Interfaces;

public interface IVenueRepository
{
    Task<List<Venue>> GetVenuesByFiltersAsync(
        string? country,
        string? city,
        string? district,
        List<string>? coveredDistricts,
        PriceLevel? maxPriceLevel,
        bool? requiresAlcohol,
        bool? requiresParking,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetAvailableCountriesAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetAvailableCitiesAsync(string? country = null, CancellationToken cancellationToken = default);
    Task<List<Venue>> GetAllVenuesAsync(CancellationToken cancellationToken = default);
    Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Venue> venues, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

public interface IGeminiAdvisorService
{
    Task<Dictionary<Guid, DateSpotAdviceResult>> GenerateDateAdvicesAsync(
        List<Venue> topVenues,
        DateConcept requestedConcept,
        string district,
        string occasion = "",
        CancellationToken cancellationToken = default);
}

public class DateSpotAdviceResult
{
    public string WhyThisSpot { get; set; } = string.Empty;
    public string IcebreakerTopic { get; set; } = string.Empty;
    public string TableTactics { get; set; } = string.Empty;
    public string IdealOrderRecommendation { get; set; } = string.Empty;
}

public interface IRevenueCatService
{
    Task<bool> IsSubscriptionActiveAsync(string appUserId, CancellationToken cancellationToken = default);
}
