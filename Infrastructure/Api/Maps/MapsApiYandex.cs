using Domain;

namespace Infrastructure.Api.Maps;

public class MapsApiYandex : IMapsApi
{
    private const string YandexMapsUrl = "https://yandex.ru/maps/?rtext=~{0}%2C{1}&rtt=mt";
    public string? GetMapLink(Location location)
    {
        return location.HasCoordinates ? 
            string.Format(YandexMapsUrl, location.Latitude, location.Longitude) : 
            null;
    }
}