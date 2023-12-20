using Domain;

namespace Infrastructure.Api.Geocode;

public interface IGeocodeApi
{
    public Location ResolveLocation(string address);
    
}