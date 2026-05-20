using CampLedger.Models;
using CampLedger.ViewModels;
using CommunityToolkit.Maui.Views;

namespace CampLedger.Pages;

public partial class TripLocationPopupPage : Popup
{
    private readonly TripLocationPopupViewModel _viewModel;
    private Window? _window;
    private string _currentMapUrl = string.Empty;

    public TripLocation? SelectedLocation { get; set; }

    public TripLocationPopupPage()
        : this(null)
    {
    }

    public TripLocationPopupPage(TripLocation? initialLocation)
    {
        InitializeComponent();
        _viewModel = new TripLocationPopupViewModel();
        BindingContext = _viewModel;
        _viewModel.LocationSelected += OnLocationSelected;

        MapsWebView.Navigating += OnWebViewNavigating;
        MapsWebView.Navigated += OnWebViewNavigated;
        InitializePopup(initialLocation, false);
    }

    public TripLocationPopupPage(TripLocation? initialLocation, bool isViewOnly)
    {
        InitializeComponent();
        _viewModel = new TripLocationPopupViewModel();
        BindingContext = _viewModel;
        _viewModel.LocationSelected += OnLocationSelected;

        MapsWebView.Navigating += OnWebViewNavigating;
        MapsWebView.Navigated += OnWebViewNavigated;
        InitializePopup(initialLocation, isViewOnly);
    }

    private void InitializePopup(TripLocation? initialLocation, bool isViewOnly)
    {
        _viewModel.InitializeLocation(initialLocation, isViewOnly);
        _currentMapUrl = _viewModel.GoogleMapsUrl;

        _window = Application.Current?.Windows.FirstOrDefault();

        if (_window == null)
        {
            return;
        }

        UpdatePopupSize();
        _window.SizeChanged += OnWindowSizeChanged;
    }

    private async void OnLocationSelected(object? sender, TripLocation? location)
    {
        SelectedLocation = location;
        DetachWindowHandler();
        await CloseAsync(CancellationToken.None);
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // Intercept Android intent:// scheme which WebView cannot handle (causes ERR_UNKNOWN_URL_SCHEME).
        // Convert to an https URL and load that instead so the embedded map works on mobile.
        if (!string.IsNullOrWhiteSpace(e.Url) && e.Url.StartsWith("intent://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // intent://www.google.com/maps/...#Intent;scheme=https;package=... -> https://www.google.com/maps/...
                var intentPart = e.Url;
                var hashIndex = intentPart.IndexOf("#", StringComparison.Ordinal);
                if (hashIndex >= 0)
                {
                    intentPart = intentPart.Substring(0, hashIndex);
                }

                // Remove the leading intent:// and prefix with https://
                var converted = "https://" + intentPart.Substring("intent://".Length);

                // Cancel the original navigation and load the converted URL instead.
                e.Cancel = true;
                MapsWebView.Source = converted;
                UpdateCurrentMapUrl(converted);
                return;
            }
            catch
            {
                // Fall through to the normal handler if conversion fails
            }
        }

        UpdateCurrentMapUrl(e.Url);
    }

    private void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        UpdateCurrentMapUrl(e.Url);
    }

    private async void OnConfirmLocationClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsViewOnly)
        {
            // Just close the popup without selecting a new location
            DetachWindowHandler();
            await CloseAsync(CancellationToken.None);
            return;
        }

        try
        {
            var locationHref = await MapsWebView.EvaluateJavaScriptAsync("window.location.href");

            if (!string.IsNullOrWhiteSpace(locationHref))
            {
                UpdateCurrentMapUrl(locationHref.Trim().Trim('\"'));
            }
        }
        catch
        {
        }

        if (!string.IsNullOrWhiteSpace(_currentMapUrl))
        {
            _viewModel.UpdateMapUrl(_currentMapUrl);
        }

        _viewModel.ConfirmLocationCommand.Execute(null);
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        // Cancel should close the popup without signaling a null selection to the view model.
        DetachWindowHandler();
        await CloseAsync(CancellationToken.None);
    }

    private void OnWindowSizeChanged(object? sender, EventArgs e)
    {
        UpdatePopupSize();
    }

    private void UpdatePopupSize()
    {
        if (_window == null)
        {
            return;
        }

        _viewModel.UpdatePopupSize(_window.Width, _window.Height);
    }

    private void DetachWindowHandler()
    {
        MapsWebView.Navigating -= OnWebViewNavigating;
        MapsWebView.Navigated -= OnWebViewNavigated;

        if (_window != null)
        {
            _window.SizeChanged -= OnWindowSizeChanged;
        }
    }

    private void UpdateCurrentMapUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        var host = uri.Host;
        var pathAndQuery = uri.PathAndQuery;

        if (!host.Contains("google.", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!pathAndQuery.Contains("/maps", StringComparison.OrdinalIgnoreCase)
            && !pathAndQuery.Contains("/place", StringComparison.OrdinalIgnoreCase)
            && !pathAndQuery.Contains("/search", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentMapUrl = uri.AbsoluteUri;
        _viewModel.UpdateMapUrl(_currentMapUrl);
    }
}
