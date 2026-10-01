using NetTopologySuite.Geometries;

namespace DateSpot.Core.Entities;

public class District
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty; // "Anadolu" veya "Avrupa"
    public Point CenterLocation { get; set; } = default!;
    public double Latitude => CenterLocation != null ? CenterLocation.Y : 0.0;
    public double Longitude => CenterLocation != null ? CenterLocation.X : 0.0;
    public List<string> PopularNeighborhoods { get; set; } = new();
    public int TotalVenuesCount { get; set; }
}
