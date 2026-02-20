namespace ToolSnap.Mobile.Services;

public class LocationService
{
    public async Task<(double Latitude, double Longitude)> GetCurrentLocationAsync()
    {
        var request = new GeolocationRequest(
            GeolocationAccuracy.Medium,
            TimeSpan.FromSeconds(10));

        var location = await Geolocation.GetLocationAsync(request);

        if (location == null)
            throw new Exception("Unable to get location");

        return (location.Latitude, location.Longitude);
    }
}
