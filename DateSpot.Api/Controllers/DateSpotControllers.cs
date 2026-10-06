using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DateSpot.Application.DTOs;
using DateSpot.Application.Services;
using DateSpot.Core.Interfaces;

namespace DateSpot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly IRevenueCatService _revenueCatService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(
        IRecommendationService recommendationService,
        IRevenueCatService revenueCatService,
        ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _revenueCatService = revenueCatService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcının filtrelerine ve yapay zeka algoritmasına göre en iyi 3 First Date mekanını üretir.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<RecommendationResponseDto>> GenerateRecommendations(
        [FromBody] RecommendationRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Öneri isteği alındı: İlçe={District}, Konsept={Concept}, Bütçe={Budget}", 
            request.District, request.Concept, request.MaxPriceLevel);

        var result = await _recommendationService.GetTopRecommendationsAsync(request, cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class VenuesController : ControllerBase
{
    private readonly IVenueRepository _venueRepository;
    private readonly DateSpot.Infrastructure.Data.DateSpotDbContext _dbContext;

    public VenuesController(
        IVenueRepository venueRepository,
        DateSpot.Infrastructure.Data.DateSpotDbContext dbContext)
    {
        _venueRepository = venueRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Veritabanında kayıtlı ülkeleri listeler.
    /// </summary>
    [HttpGet("countries")]
    public async Task<ActionResult<List<string>>> GetCountries(CancellationToken cancellationToken)
    {
        var countries = await _venueRepository.GetAvailableCountriesAsync(cancellationToken);
        return Ok(countries);
    }

    /// <summary>
    /// Belirli bir ülkedeki veya tüm dünyadaki kayıtlı şehirleri listeler.
    /// </summary>
    [HttpGet("cities")]
    public async Task<ActionResult<List<string>>> GetCities([FromQuery] string? country, CancellationToken cancellationToken)
    {
        var cities = await _venueRepository.GetAvailableCitiesAsync(country, cancellationToken);
        return Ok(cities);
    }

    /// <summary>
    /// Şehir ve ülkeye göre ilçeleri ve popüler semtleri listeler.
    /// </summary>
    [HttpGet("districts")]
    public async Task<ActionResult<List<object>>> GetDistricts(
        [FromQuery] string? country,
        [FromQuery] string? city,
        [FromQuery] string? zone,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Districts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(country))
        {
            query = query.Where(d => d.Country.ToLower() == country.ToLower() || d.CountryCode.ToLower() == country.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(d => d.City.ToLower() == city.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(zone))
        {
            query = query.Where(d => d.Zone.ToLower() == zone.ToLower());
        }

        var districts = await query
            .OrderBy(d => d.City)
            .ThenBy(d => d.Zone)
            .ThenBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Country,
                d.CountryCode,
                d.City,
                d.StateOrRegion,
                d.Name,
                d.Zone,
                Side = d.Zone, // Geriye dönük mobil uyumluluk
                d.Latitude,
                d.Longitude,
                d.PopularNeighborhoods,
                d.TotalVenuesCount,
                d.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(districts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueRecommendationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var venue = await _venueRepository.GetByIdAsync(id, cancellationToken);
        if (venue == null)
            return NotFound();

        return Ok(venue);
    }

    /// <summary>
    /// Veritabanına manuel yeni bir mekan ekler (Swagger veya API üzerinden).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<object>> CreateVenue(
        [FromBody] CreateVenueDto dto,
        CancellationToken cancellationToken)
    {
        int? districtId = dto.DistrictId;
        if (!districtId.HasValue && !string.IsNullOrWhiteSpace(dto.District))
        {
            var matchedDistrict = await _dbContext.Districts
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Name.ToLower() == dto.District.ToLower(), cancellationToken);
            districtId = matchedDistrict?.Id;
        }

        var venue = new DateSpot.Core.Entities.Venue
        {
            Name = dto.Name,
            DistrictId = districtId,
            Country = string.IsNullOrWhiteSpace(dto.Country) ? "Türkiye" : dto.Country,
            CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "TR" : dto.CountryCode,
            City = string.IsNullOrWhiteSpace(dto.City) ? "İstanbul" : dto.City,
            StateOrRegion = dto.StateOrRegion ?? string.Empty,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            Address = dto.Address,
            PostalCode = dto.PostalCode ?? string.Empty,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TRY" : dto.Currency,
            TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone) ? "Europe/Istanbul" : dto.TimeZone,
            GoogleRating = dto.GoogleRating,
            ReviewCount = dto.ReviewCount,
            HeroImageUrl = dto.HeroImageUrl,
            GalleryImages = dto.GalleryImages,
            GoogleMapsUrl = dto.GoogleMapsUrl,
            InstagramHandle = dto.InstagramHandle,
            WebsiteUrl = dto.WebsiteUrl,
            PhoneNumber = dto.PhoneNumber,

            NoiseLevel = dto.NoiseLevel,
            LightingStyle = dto.LightingStyle,
            MusicProfile = dto.MusicProfile,
            DressCode = dto.DressCode,
            ViewType = dto.ViewType,

            PriceLevel = dto.PriceLevel,
            CuisineTypes = dto.CuisineTypes,
            MealTimes = dto.MealTimes,
            SignatureItems = dto.SignatureItems,
            DietaryOptions = dto.DietaryOptions,

            SeatingTypes = dto.SeatingTypes,
            TableSpacing = dto.TableSpacing,
            BestTableTip = dto.BestTableTip,
            SeatingArrangement = dto.SeatingArrangement,

            HasAlcohol = dto.HasAlcohol,
            HasOutdoorSeating = dto.HasOutdoorSeating,
            HasValetParking = dto.HasValetParking,
            RequiresReservation = dto.RequiresReservation,
            IsPetFriendly = dto.IsPetFriendly,
            HasWifiAndSockets = dto.HasWifiAndSockets,
            SmokingArea = dto.SmokingArea,

            SuitableOccasions = dto.SuitableOccasions,
            VibeTags = dto.VibeTags,
            BestTimeToVisit = dto.BestTimeToVisit,
            FirstDateSuitabilityScore = dto.FirstDateSuitabilityScore,
            RawMetadata = dto.RawMetadata
        };

        await _dbContext.Venues.AddAsync(venue, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = venue.Id }, new 
        { 
            id = venue.Id, 
            name = venue.Name, 
            districtId = venue.DistrictId,
            country = venue.Country,
            city = venue.City,
            district = venue.District,
            message = "Mekan başarıyla veritabanına eklendi." 
        });
    }

    /// <summary>
    /// Birden fazla mekanı topluca (bulk) veritabanına ekler.
    /// </summary>
    [HttpPost("bulk")]
    public async Task<ActionResult<object>> CreateVenuesBulk(
        [FromBody] List<CreateVenueDto> dtoList,
        CancellationToken cancellationToken)
    {
        var allDistricts = await _dbContext.Districts.AsNoTracking().ToListAsync(cancellationToken);
        var districtMap = allDistricts.ToDictionary(d => d.Name.ToLower(), d => d.Id);

        var venueList = dtoList.Select(dto =>
        {
            int? matchedId = dto.DistrictId;
            if (!matchedId.HasValue && !string.IsNullOrWhiteSpace(dto.District) && districtMap.TryGetValue(dto.District.ToLower(), out int dId))
            {
                matchedId = dId;
            }

            return new DateSpot.Core.Entities.Venue
            {
                Name = dto.Name,
                DistrictId = matchedId,
                Country = string.IsNullOrWhiteSpace(dto.Country) ? "Türkiye" : dto.Country,
                CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? "TR" : dto.CountryCode,
                City = string.IsNullOrWhiteSpace(dto.City) ? "İstanbul" : dto.City,
                StateOrRegion = dto.StateOrRegion ?? string.Empty,
                District = dto.District,
                Neighborhood = dto.Neighborhood,
                Address = dto.Address,
                PostalCode = dto.PostalCode ?? string.Empty,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "TRY" : dto.Currency,
                TimeZone = string.IsNullOrWhiteSpace(dto.TimeZone) ? "Europe/Istanbul" : dto.TimeZone,
                GoogleRating = dto.GoogleRating,
                ReviewCount = dto.ReviewCount,
                HeroImageUrl = dto.HeroImageUrl,
                GalleryImages = dto.GalleryImages,
                GoogleMapsUrl = dto.GoogleMapsUrl,
                InstagramHandle = dto.InstagramHandle,
                WebsiteUrl = dto.WebsiteUrl,
                PhoneNumber = dto.PhoneNumber,

                NoiseLevel = dto.NoiseLevel,
                LightingStyle = dto.LightingStyle,
                MusicProfile = dto.MusicProfile,
                DressCode = dto.DressCode,
                ViewType = dto.ViewType,

                PriceLevel = dto.PriceLevel,
                CuisineTypes = dto.CuisineTypes,
                MealTimes = dto.MealTimes,
                SignatureItems = dto.SignatureItems,
                DietaryOptions = dto.DietaryOptions,

                SeatingTypes = dto.SeatingTypes,
                TableSpacing = dto.TableSpacing,
                BestTableTip = dto.BestTableTip,
                SeatingArrangement = dto.SeatingArrangement,

                HasAlcohol = dto.HasAlcohol,
                HasOutdoorSeating = dto.HasOutdoorSeating,
                HasValetParking = dto.HasValetParking,
                RequiresReservation = dto.RequiresReservation,
                IsPetFriendly = dto.IsPetFriendly,
                HasWifiAndSockets = dto.HasWifiAndSockets,
                SmokingArea = dto.SmokingArea,

                SuitableOccasions = dto.SuitableOccasions,
                VibeTags = dto.VibeTags,
                BestTimeToVisit = dto.BestTimeToVisit,
                FirstDateSuitabilityScore = dto.FirstDateSuitabilityScore,
                RawMetadata = dto.RawMetadata
            };
        }).ToList();

        await _dbContext.Venues.AddRangeAsync(venueList, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new 
        { 
            count = venueList.Count, 
            message = $"{venueList.Count} mekan başarıyla veritabanına eklendi." 
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly IRevenueCatService _revenueCatService;

    public SubscriptionController(IRevenueCatService revenueCatService)
    {
        _revenueCatService = revenueCatService;
    }

    [HttpGet("verify/{appUserId}")]
    public async Task<ActionResult<object>> VerifySubscription(string appUserId, CancellationToken cancellationToken)
    {
        bool isActive = await _revenueCatService.IsSubscriptionActiveAsync(appUserId, cancellationToken);
        return Ok(new
        {
            AppUserId = appUserId,
            IsSubscriptionActive = isActive,
            Tier = isActive ? "Monthly_VIP_Pass" : "None"
        });
    }
}
