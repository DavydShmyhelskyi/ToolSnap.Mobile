using System.Globalization;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class MapPage : ContentPage
{
    private readonly UserSessionService _session;
    private readonly FindToolsForMapService _mapService;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private Guid? _selectedToolTypeId;
    private Guid? _selectedBrandId;
    private ToolAvailabilityFilter _availability = ToolAvailabilityFilter.All;
    private string _search = "";

    private List<MapMarkerDto> _resultMarkers = [];
    private int _resultIndex;

    public MapPage(UserSessionService session, FindToolsForMapService mapService)
    {
        InitializeComponent();
        _session = session;
        _mapService = mapService;

        ToolTypePicker.SelectedIndexChanged += (_, _) =>
        {
            _selectedToolTypeId = ToolTypePicker.SelectedItem is ToolTypeDto t ? t.Id : null;
            _ = LoadMapAsync();
        };

        BrandPicker.SelectedIndexChanged += (_, _) =>
        {
            _selectedBrandId = BrandPicker.SelectedItem is BrandDto b ? b.Id : null;
            _ = LoadMapAsync();
        };

        AvailabilityPicker.SelectedIndexChanged += (_, _) =>
        {
            _availability = AvailabilityPicker.SelectedItem switch
            {
                "Available"     => ToolAvailabilityFilter.Available,
                "Not available" => ToolAvailabilityFilter.NotAvailable,
                _               => ToolAvailabilityFilter.All
            };
            _ = LoadMapAsync();
        };

        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        try
        {
            var typesTask  = _mapService.GetToolTypesAsync();
            var brandsTask = _mapService.GetBrandsAsync();
            await Task.WhenAll(typesTask, brandsTask);

            ToolTypePicker.ItemsSource         = typesTask.Result;
            ToolTypePicker.ItemDisplayBinding  = new Binding("Title");
            BrandPicker.ItemsSource            = brandsTask.Result;
            BrandPicker.ItemDisplayBinding     = new Binding("Title");

            await LoadMapAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async Task LoadMapAsync()
    {
        try
        {
            var markers = await _mapService.LoadMarkersAsync(
                _availability, _selectedToolTypeId, _selectedBrandId, null);

            if (!string.IsNullOrWhiteSpace(_search))
            {
                markers = markers
                    .Where(m =>
                        m.Title.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                        m.Subtitle.Contains(_search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            double userLat = _session.CurrentUser?.Latitude  ?? 50.4501;
            double userLon = _session.CurrentUser?.Longitude ?? 30.5234;

            _resultMarkers = [.. markers.OrderBy(m => Dist(userLat, userLon, m.Latitude, m.Longitude))];
            _resultIndex   = 0;

            RenderWebView();
            UpdateNavigationUI();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private void RenderWebView()
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        double userLat = _session.CurrentUser?.Latitude  ?? 50.4501;
        double userLon = _session.CurrentUser?.Longitude ?? 30.5234;
        double centerLat = _resultMarkers.Count > 0 ? _resultMarkers[_resultIndex].Latitude  : userLat;
        double centerLon = _resultMarkers.Count > 0 ? _resultMarkers[_resultIndex].Longitude : userLon;
        string markersJson = JsonSerializer.Serialize(_resultMarkers, _jsonOpts);
        MapWebView.Source = new HtmlWebViewSource
        {
            Html = BuildHtml(centerLat, centerLon, markersJson, _resultMarkers.Count > 0, isDark)
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Application.Current!.RequestedThemeChanged += OnThemeChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Application.Current!.RequestedThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => RenderWebView();

    private void OnSearchPressed(object? sender, EventArgs e)
    {
        _search = SearchBar.Text?.Trim() ?? "";
        _ = LoadMapAsync();
    }

    private void OnClearSearch(object? sender, EventArgs e)
    {
        SearchBar.Text = "";
        _search        = "";
        _ = LoadMapAsync();
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        if (_resultMarkers.Count < 2) return;
        _resultIndex = (_resultIndex + 1) % _resultMarkers.Count;
        await MapWebView.EvaluateJavaScriptAsync($"moveToIndex({_resultIndex})");
        UpdateNavigationUI();
    }

    private void UpdateNavigationUI()
    {
        int count = _resultMarkers.Count;
        NextButton.IsVisible = count > 1;
        ResultLabel.Text = count switch
        {
            0 => "No results",
            1 => "1 result",
            _ => $"{count} results  ({_resultIndex + 1}/{count})"
        };
    }

    private static double Dist(double lat1, double lon1, double lat2, double lon2) =>
        (lat1 - lat2) * (lat1 - lat2) + (lon1 - lon2) * (lon1 - lon2);

    private static string BuildHtml(double lat, double lon, string markersJson, bool hasResults, bool isDark = false)
    {
        string sLat = lat.ToString(CultureInfo.InvariantCulture);
        string sLon = lon.ToString(CultureInfo.InvariantCulture);
        string darkCss = isDark
            ? "html,body,#map,.leaflet-container{background:#1F1F1F;}"
            : "";

        return $@"<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='initial-scale=1.0,maximum-scale=1.0'>
<style>
  html,body{{height:100%;margin:0;padding:0;}}
  #map{{height:100%;width:100%;}}
  {darkCss}
</style>
<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.3/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.3/dist/leaflet.js'></script>
</head>
<body>
<div id='map'></div>
<script>
var map = L.map('map').setView([{sLat},{sLon}],13);

L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png',{{
    attribution:'© OpenStreetMap contributors'
}}).addTo(map);

var data = {markersJson};
var lms  = [];

data.forEach(function(m) {{
    var lm = L.marker([m.latitude, m.longitude], {{
        icon: L.icon({{ iconUrl: iconFor(m.kind), iconSize: [30,30] }})
    }})
    .addTo(map)
    .bindPopup('<b>' + m.title + '</b><br/>' + m.subtitle);
    lms.push(lm);
}});

function iconFor(k) {{
    if (k === 0) return 'https://cdn-icons-png.flaticon.com/512/149/149071.png';
    if (k === 1) return 'https://cdn-icons-png.flaticon.com/512/684/684908.png';
    return 'https://cdn-icons-png.flaticon.com/512/891/891448.png';
}}

function moveToIndex(i) {{
    if (i < 0 || i >= data.length) return;
    map.setView([data[i].latitude, data[i].longitude], 16);
    lms[i].openPopup();
}}

{(hasResults ? "moveToIndex(0);" : "")}
</script>
</body>
</html>";
    }
}
