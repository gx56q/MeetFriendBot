using Newtonsoft.Json;

namespace Domain;

public class Location
{
    public readonly string? Loc;
    public string? Longitude { get; set; }
    public string? Latitude { get; set; }
    public bool HasCoordinates => Latitude != null && Longitude != null;
    public Location(string? loc) => Loc = loc;
    public Location(string loc, string? longitude, string? latitude) : this(loc)
    {
        Latitude = latitude;
        Longitude = longitude;
    }
    
    [JsonConstructor]
    public Location(string? loc, string? longitude, string? latitude, bool hasCoordinates)
    {
        Loc = loc;
        Longitude = longitude;
        Latitude = latitude;
    }
    
    public string GetLocation()
    {
        if (Latitude == null || Longitude == null)
        {
            return Loc ?? "";
        }
        return $"{Latitude},{Longitude}";
    }
    
}