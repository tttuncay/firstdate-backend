using NetTopologySuite.Geometries;
using DateSpot.Core.Enums;

namespace DateSpot.Core.Entities;

public class Venue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty; // Kadıköy, Beşiktaş, Beyoğlu, Sarıyer, vb.
    public string Neighborhood { get; set; } = string.Empty; // Moda, Akaretler, Cihangir, vb.
    public string Address { get; set; } = string.Empty;
    
    // PostGIS Coğrafi Noktası (SRID 4326: WGS 84 - GPS koordinatları)
    public Point Location { get; set; } = default!;
    public double Latitude => Location != null ? Location.Y : 0.0;
    public double Longitude => Location != null ? Location.X : 0.0;

    public PriceLevel PriceLevel { get; set; }
    public NoiseLevel NoiseLevel { get; set; }
    public List<DateConcept> CompatibleConcepts { get; set; } = new();
    public List<string> VibeTags { get; set; } = new(); // ["Mum Işığı", "Caz Müzik", "Ferah Masalar", "Boğaz Manzarası"]

    public string SeatingArrangement { get; set; } = string.Empty; // "Geniş Masalar, Rahat Berjerler"
    public bool HasAlcohol { get; set; }
    public bool HasValetParking { get; set; }
    public bool RequiresReservation { get; set; }
    public bool HasOutdoorSeating { get; set; }

    public double FirstDateSuitabilityScore { get; set; } // 1.0 - 10.0 arası First Date uygunluk puanı
    public double GoogleRating { get; set; }
    public int ReviewCount { get; set; }

    public string HeroImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string InstagramHandle { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
