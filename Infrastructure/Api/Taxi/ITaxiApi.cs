using Domain;

namespace Infrastructure.Api.Taxi;

public interface ITaxiApi
{
    public string? GetTaxiLink(Location location);
}