using Domain;

namespace Infrastructure.Api.Taxi;

public class TaxiApiYandex : ITaxiApi
{
    private const string TariffClass = "comfortplus";
    private const string Reference = "MeetMateBot";
    private const string Language = "ru";
    private string AppmetricaTrackingId { get; }
    private const string YandexTaxiUrl = "https://3.redirect.appmetrica.yandex.com/route?&end-lat={0}&end-lon={1}&tariffClass={2}&ref={3}&appmetrica_tracking_id={4}&lang={5}";
    
    public TaxiApiYandex(string appmetricaTrackingId)
    {
        AppmetricaTrackingId = appmetricaTrackingId;
    }
    public string? GetTaxiLink(Location location)
    {
        return location.HasCoordinates ? 
            string.Format(YandexTaxiUrl, location.Latitude, location.Longitude, TariffClass,
                Reference, AppmetricaTrackingId, Language) : 
            null;
    }
}