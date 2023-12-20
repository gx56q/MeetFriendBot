using Domain;

namespace Infrastructure.Api.Maps;

public interface IMapsApi
{
    public string? GetMapLink(Location location);
}