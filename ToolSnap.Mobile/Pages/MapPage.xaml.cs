using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages
{
    public partial class MapPage : ContentPage
    {
        private readonly UserSessionService _session;
        private readonly FindToolsForMapService _mapService;

        // 🔧 Опції для серіалізації маркерів у camelCase
        private static readonly JsonSerializerOptions MarkerJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Фільтри
        private Guid? _selectedToolTypeId = null;
        private Guid? _selectedBrandId = null;
        // private Guid? _selectedModelId = null; // 🔸 більше не використовуємо
        private ToolAvailabilityFilter _availability = ToolAvailabilityFilter.All;
        private string _search = "";

        public MapPage(UserSessionService session, FindToolsForMapService mapService)
        {
            InitializeComponent();
            _session = session;
            _mapService = mapService;

            // Підписка на події
            ToolTypePicker.SelectedIndexChanged += ToolTypePicker_SelectedIndexChanged;
            BrandPicker.SelectedIndexChanged += BrandPicker_SelectedIndexChanged;
            // ModelPicker.SelectedIndexChanged += ModelPicker_SelectedIndexChanged; // 🔸 прибрали з XAML
            AvailabilityPicker.SelectedIndexChanged += AvailabilityPicker_SelectedIndexChanged;
            SearchBar.TextChanged += SearchBar_TextChanged;

            // Стартове завантаження
            _ = InitializeFiltersAndLoadMapAsync();
        }

        // ========================================================
        // INITIAL LOAD (FILL PICKERS + LOAD MAP)
        // ========================================================
        private async Task InitializeFiltersAndLoadMapAsync()
        {
            try
            {
                // Завантажуємо фільтри
                var toolTypes = await _mapService.GetToolTypesAsync();
                var brands = await _mapService.GetBrandsAsync();
                // var models    = await _mapService.GetModelsAsync(); // 🔸 модель не фільтруємо

                ToolTypePicker.ItemsSource = toolTypes;
                ToolTypePicker.ItemDisplayBinding = new Binding("Title");

                BrandPicker.ItemsSource = brands;
                BrandPicker.ItemDisplayBinding = new Binding("Title");

                // ModelPicker.ItemsSource = models;                     // 🔸 немає в XAML
                // ModelPicker.ItemDisplayBinding = new Binding("Title"); // 🔸 немає в XAML

                // AvailabilityPicker (у XAML вже заданий)

                await LoadMapAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // ========================================================
        // LOAD MAP WITH FILTERS
        // ========================================================
        private async Task LoadMapAsync()
        {
            var markers = await _mapService.LoadMarkersAsync(
                _availability,
                _selectedToolTypeId,
                _selectedBrandId,
                null // без моделі
            );

            // 🔥 Пошук за текстом
            if (!string.IsNullOrWhiteSpace(_search))
            {
                markers = markers
                    .Where(m => m.Title.Contains(_search, StringComparison.OrdinalIgnoreCase)
                             || m.Subtitle.Contains(_search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // --------------------------
            // 🔔 СПОВІЩЕННЯ ДЛЯ КОРИСТУВАЧА
            // --------------------------
            int toolsCount = markers.Count(m => m.Kind == MapMarkerKind.Tool);

            string msg = $"Знайдено інструментів: {toolsCount}";

            if (_selectedToolTypeId == null && _selectedBrandId == null)
                msg += "\n(No filters: ToolType = null, Brand = null)";

            if (_selectedToolTypeId == null)
                msg += "\n⚠️ ToolType не вибрано";

            if (_selectedBrandId == null)
                msg += "\n⚠️ Brand не вибрано";

            await DisplayAlertAsync("Search Result", msg, "OK");

            // --------------------------
            // 🔥 ПОБУДОВА КАРТИ
            // --------------------------

            // ✅ Серіалізуємо маркери у camelCase, щоб у JS були поля kind/latitude/longitude/title/subtitle
            string markersJson = JsonSerializer.Serialize(markers, MarkerJsonOptions);

            double lat = _session.CurrentUser?.Latitude ?? 50.4501;
            double lon = _session.CurrentUser?.Longitude ?? 30.5234;

            string html = BuildHtml(lat, lon, markersJson);

            MapWebView.Source = new HtmlWebViewSource { Html = html };
        }

        // ========================================================
        // HTML BUILDER (MAP + MARKERS)
        // ========================================================
        private string BuildHtml(double lat, double lon, string markersJson)
        {
            string safeLat = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string safeLon = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta name='viewport' content='initial-scale=1.0, maximum-scale=1.0'>
<style>
    html, body {{ height: 100%; margin: 0; padding: 0; }}
    #map {{ height: 100%; width: 100%; }}
</style>

<link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.3/dist/leaflet.css'/>
<script src='https://unpkg.com/leaflet@1.9.3/dist/leaflet.js'></script>

</head>
<body>

<div id='map'></div>

<script>

var map = L.map('map').setView([{safeLat}, {safeLon}], 14);

L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
    attribution: '© OpenStreetMap contributors'
}}).addTo(map);

var markers = {markersJson};

markers.forEach(m => {{
    var icon = L.icon({{
        iconUrl: getIcon(m.kind),
        iconSize: [30, 30]
    }});

    L.marker([m.latitude, m.longitude], {{ icon: icon }})
        .addTo(map)
        .bindPopup(`<b>${{m.title}}</b><br/>${{m.subtitle}}`);
}});

function getIcon(kind) {{
    if (kind === 0) return 'https://cdn-icons-png.flaticon.com/512/149/149071.png'; // User
    if (kind === 1) return 'https://cdn-icons-png.flaticon.com/512/684/684908.png'; // Location
    return 'https://cdn-icons-png.flaticon.com/512/891/891448.png'; // Tool
}}

</script>

</body>
</html>";
        }

        // ========================================================
        // UI EVENTS AND FILTER LOGIC
        // ========================================================

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _search = e.NewTextValue ?? "";
            _ = LoadMapAsync();
        }

        private void OnClearSearch(object sender, EventArgs e)
        {
            SearchBar.Text = "";
            _search = "";
            _ = LoadMapAsync();
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            _ = LoadMapAsync();
        }

        private void ToolTypePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ToolTypePicker.SelectedItem is ToolTypeDto item)
                _selectedToolTypeId = item.Id;
            else
                _selectedToolTypeId = null;

            _ = LoadMapAsync();
        }

        private void BrandPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (BrandPicker.SelectedItem is BrandDto item)
                _selectedBrandId = item.Id;
            else
                _selectedBrandId = null;

            _ = LoadMapAsync();
        }

        // private void ModelPicker_SelectedIndexChanged(object sender, EventArgs e)
        // {
        //     if (ModelPicker.SelectedItem is ModelDto item)
        //         _selectedModelId = item.Id;
        //     else
        //         _selectedModelId = null;
        //
        //     _ = LoadMapAsync();
        // }

        private void AvailabilityPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AvailabilityPicker.SelectedItem is string text)
            {
                _availability = text switch
                {
                    "Available" => ToolAvailabilityFilter.Available,
                    "NotAvailable" => ToolAvailabilityFilter.NotAvailable,
                    _ => ToolAvailabilityFilter.All
                };
            }

            _ = LoadMapAsync();
        }
    }
}