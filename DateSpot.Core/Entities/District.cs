namespace DateSpot.Core.Entities;

public class District
{
    public int Id { get; set; }
    public string Country { get; set; } = "Türkiye";
    public string CountryCode { get; set; } = "TR";
    public string City { get; set; } = "İstanbul";
    public string Name { get; set; } = string.Empty; // "Üsküdar", "Manhattan", "Westminster", "Shinjuku"
    public string StateOrRegion { get; set; } = string.Empty; // "Marmara", "New York", "Greater London"
    public string Zone { get; set; } = string.Empty; // Evrensel Bölge/Yaka: "Anadolu Yakası", "Avrupa Yakası", "Downtown", "West End", vb.
    public string Side { get => Zone; set => Zone = value; } // Geriye dönük uyumluluk köprüsü
    public double Latitude { get; set; } = 0.0;
    public double Longitude { get; set; } = 0.0;
    public List<string> PopularNeighborhoods { get; set; } = new();
    public int TotalVenuesCount { get; set; }
    public bool IsActive { get; set; } = true;
}
