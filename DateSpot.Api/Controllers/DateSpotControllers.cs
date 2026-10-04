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
    /// İstanbul'un tüm 39 ilçesini koordinatları ve popüler semtleriyle listeler.
    /// </summary>
    [HttpGet("districts")]
    public async Task<ActionResult<List<object>>> GetDistricts(CancellationToken cancellationToken)
    {
        var districts = await _dbContext.Districts
            .AsNoTracking()
            .OrderBy(d => d.Side)
            .ThenBy(d => d.Name)
            .Select(d => new
            {
                d.Id,
                d.Name,
                d.Side,
                d.Latitude,
                d.Longitude,
                d.PopularNeighborhoods
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
        var gf = new NetTopologySuite.Geometries.GeometryFactory(new NetTopologySuite.Geometries.PrecisionModel(), 4326);

        var venue = new DateSpot.Core.Entities.Venue
        {
            Name = dto.Name,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            Address = dto.Address,
            Location = gf.CreatePoint(new NetTopologySuite.Geometries.Coordinate(dto.Longitude, dto.Latitude)),
            PriceLevel = dto.PriceLevel,
            NoiseLevel = dto.NoiseLevel,
            CompatibleConcepts = dto.CompatibleConcepts,
            VibeTags = dto.VibeTags,
            SeatingArrangement = dto.SeatingArrangement,
            HasAlcohol = dto.HasAlcohol,
            HasValetParking = dto.HasValetParking,
            RequiresReservation = dto.RequiresReservation,
            HasOutdoorSeating = dto.HasOutdoorSeating,
            FirstDateSuitabilityScore = dto.FirstDateSuitabilityScore,
            GoogleRating = dto.GoogleRating,
            ReviewCount = dto.ReviewCount,
            HeroImageUrl = dto.HeroImageUrl,
            GalleryImages = dto.GalleryImages,
            GoogleMapsUrl = dto.GoogleMapsUrl,
            InstagramHandle = dto.InstagramHandle
        };

        await _dbContext.Venues.AddAsync(venue, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = venue.Id }, new 
        { 
            id = venue.Id, 
            name = venue.Name, 
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
        var gf = new NetTopologySuite.Geometries.GeometryFactory(new NetTopologySuite.Geometries.PrecisionModel(), 4326);

        var venueList = dtoList.Select(dto => new DateSpot.Core.Entities.Venue
        {
            Name = dto.Name,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            Address = dto.Address,
            Location = gf.CreatePoint(new NetTopologySuite.Geometries.Coordinate(dto.Longitude, dto.Latitude)),
            PriceLevel = dto.PriceLevel,
            NoiseLevel = dto.NoiseLevel,
            CompatibleConcepts = dto.CompatibleConcepts,
            VibeTags = dto.VibeTags,
            SeatingArrangement = dto.SeatingArrangement,
            HasAlcohol = dto.HasAlcohol,
            HasValetParking = dto.HasValetParking,
            RequiresReservation = dto.RequiresReservation,
            HasOutdoorSeating = dto.HasOutdoorSeating,
            FirstDateSuitabilityScore = dto.FirstDateSuitabilityScore,
            GoogleRating = dto.GoogleRating,
            ReviewCount = dto.ReviewCount,
            HeroImageUrl = dto.HeroImageUrl,
            GalleryImages = dto.GalleryImages,
            GoogleMapsUrl = dto.GoogleMapsUrl,
            InstagramHandle = dto.InstagramHandle
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
