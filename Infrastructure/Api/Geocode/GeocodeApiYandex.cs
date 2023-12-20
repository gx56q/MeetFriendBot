using Domain;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Api.Geocode;

public class GeocodeApiYandex  : IGeocodeApi
{
    private string ApiKey { get; set; }

    public GeocodeApiYandex(string apiKey)
    {
        ApiKey = apiKey;
    }
    
    public Location ResolveLocation(string location)
    {
        const string url = "https://geocode-maps.yandex.ru/1.x/";
        var parameters = new Dictionary<string, string>
        {
            ["apikey"] = ApiKey,
            ["format"] = "json",
            ["geocode"] = location,
            ["results"] = "1",
            ["lang"] = "ru_RU"
        };
        var response = GetResponse(url, parameters);
        var jObject = JObject.Parse(response);
        var isFound = jObject["response"]?["GeoObjectCollection"]?["metaDataProperty"]?["GeocoderResponseMetaData"]?["found"]?.Value<int>() > 0;
        if (!isFound)
        {
            return new Location(location);
        }
        var geoObject = jObject["response"]?["GeoObjectCollection"]?["featureMember"]?.FirstOrDefault();
        var point = geoObject?["GeoObject"]?["Point"]?["pos"].ToString();
        var coordinates = point?.Split(' ');
        if (coordinates is not { Length: 2 })
        {
            return new Location(location);
        }
        location = geoObject?["GeoObject"]?["metaDataProperty"]?["GeocoderMetaData"]?["text"]?.ToString() ?? location;
        var longitude = coordinates[0];
        var latitude = coordinates[1];
        return new Location(location, longitude, latitude);
    }
    
    private static string GetResponse(string url, Dictionary<string, string> parameters)
    {
        var queryString = string.Join("&",
            parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var fullUrl = $"{url}?{queryString}";

        using var httpClient = new HttpClient();
        var response = httpClient.GetStringAsync(fullUrl).Result;
        return response;
    }
}