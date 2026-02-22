namespace ToolSnap.Mobile.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnTakeClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Take", "Navigate to Take Page", "OK");
        await Shell.Current.GoToAsync(nameof(TakePage));
    }

    private async void OnReturnClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Return", "Navigate to Return Page", "OK");
        await Shell.Current.GoToAsync(nameof(ReturnPage));
    }
    private async void OnMapClicked(object sender, EventArgs e)
    {
        
        // await Shell.Current.GoToAsync("profile1");
        try
        {
            await Shell.Current.GoToAsync(nameof(MapPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Navigation error", ex.ToString(), "OK");
        }
    }
}
