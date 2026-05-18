namespace ToolSnap.Mobile.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnTakeClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TakePage));
    }

    private async void OnReturnClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ReturnPage));
    }

    private async void OnMapClicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(MapPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Navigation error", ex.Message, "OK");
        }
    }
}
