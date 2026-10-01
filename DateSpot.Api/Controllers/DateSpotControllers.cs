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
