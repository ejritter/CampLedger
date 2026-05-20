using CampLedger.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class TripLocationPopupViewModel : ViewModelBase
{
    private const string DefaultGoogleMapsUrl = "https://www.google.com/maps/";

    [ObservableProperty]
    public partial string GoogleMapsUrl { get; set; } = DefaultGoogleMapsUrl;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsNotLoading { get; set; } = true;

    [ObservableProperty]
    public partial TripLocation? SelectedLocation { get; set; }

    [ObservableProperty]
    public partial bool IsViewOnly { get; set; }

    [ObservableProperty]
    public partial double PopupWidth { get; set; } = 960;

    [ObservableProperty]
    public partial double PopupHeight { get; set; } = 720;

    [ObservableProperty]
    public partial bool HasSavedLocation { get; set; }

    [ObservableProperty]
    public partial string LocationButtonText { get; set; } = "📍 Confirm";

    public event EventHandler<TripLocation?>? LocationSelected;

    public TripLocationPopupViewModel()
    {
    }

    [RelayCommand]
    private void ConfirmLocation()
    {
        var mapsUrl = GoogleMapsUrl.Trim();

        if (string.IsNullOrWhiteSpace(mapsUrl) || mapsUrl == DefaultGoogleMapsUrl)
        {
            return;
        }

        var locationName = ExtractLocationName(mapsUrl);
        
        SelectedLocation = new TripLocation
        {
            LocationName = locationName,
            GoogleMapsUrl = mapsUrl
        };

        LocationSelected?.Invoke(this, SelectedLocation);
    }

    [RelayCommand]
    private void ClosePopup()
    {
        LocationSelected?.Invoke(this, null);
    }

    public void UpdateMapUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        GoogleMapsUrl = url;
    }

    public void UpdateSelectedLocation(TripLocation location)
    {
        SelectedLocation = location;
        GoogleMapsUrl = location.GoogleMapsUrl;
    }

    partial void OnIsLoadingChanged(bool value)
    {
        IsNotLoading = !value;
    }

    partial void OnSelectedLocationChanged(TripLocation? value)
    {
        UpdateHasSavedLocation();
        UpdateLocationButtonText();
    }

    partial void OnGoogleMapsUrlChanged(string value)
    {
        UpdateHasSavedLocation();
    }

    public void InitializeLocation(TripLocation? location)
    {
        InitializeLocation(location, false);
    }

    public void InitializeLocation(TripLocation? location, bool isViewOnly)
    {
        IsViewOnly = isViewOnly;
        SelectedLocation = location;

        if (!string.IsNullOrWhiteSpace(location?.GoogleMapsUrl))
        {
            GoogleMapsUrl = location.GoogleMapsUrl;
        }
        else
        {
            GoogleMapsUrl = DefaultGoogleMapsUrl;
        }

        UpdateHasSavedLocation();
        UpdateLocationButtonText();
    }

    public void UpdatePopupSize(double windowWidth, double windowHeight)
    {
        if (windowWidth <= 0 || windowHeight <= 0)
        {
            return;
        }

        var calculatedWidth = Math.Max(320, windowWidth * 0.8d);
        var calculatedHeight = Math.Max(360, windowHeight * 0.8d);

        PopupWidth = calculatedWidth;
        PopupHeight = calculatedHeight;
    }

    private void UpdateHasSavedLocation()
    {
        HasSavedLocation = !string.IsNullOrWhiteSpace(SelectedLocation?.GoogleMapsUrl)
            || !string.IsNullOrWhiteSpace(GoogleMapsUrl) && GoogleMapsUrl != DefaultGoogleMapsUrl;
    }

    private void UpdateLocationButtonText()
    {
        if (IsViewOnly)
        {
            LocationButtonText = "Close";
            return;
        }

        LocationButtonText = "📍 Confirm";
    }

    private static string ExtractLocationName(string url)
    {
        const string placeMarker = "/maps/place/";
        var markerIndex = url.IndexOf(placeMarker, StringComparison.OrdinalIgnoreCase);

        if (markerIndex >= 0)
        {
            var nameStartIndex = markerIndex + placeMarker.Length;
            var nameEndIndex = url.IndexOf('/', nameStartIndex);

            if (nameEndIndex < 0)
            {
                nameEndIndex = url.IndexOf('?', nameStartIndex);
            }

            if (nameEndIndex < 0)
            {
                nameEndIndex = url.Length;
            }

            var encodedName = url.Substring(nameStartIndex, nameEndIndex - nameStartIndex);
            var locationName = Uri.UnescapeDataString(encodedName.Replace('+', ' '));

            if (!string.IsNullOrWhiteSpace(locationName))
            {
                return locationName;
            }
        }

        return "Something went wrong..";
    }
}
