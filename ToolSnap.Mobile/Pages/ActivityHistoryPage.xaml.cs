using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class ActivityHistoryPage : ContentPage
{
    private readonly ActivityHistoryService _service;

    public ActivityHistoryPage(ActivityHistoryService service)
    {
        InitializeComponent();
        _service = service;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadAsync();

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadAsync();
        PageRefreshView.IsRefreshing = false;
    }

    private async Task LoadAsync()
    {
        try
        {
            var items = await _service.LoadAsync();
            HistoryList.ItemsSource = items;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}
