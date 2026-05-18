using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace ToolSnap.Mobile.Services;

public static class AppToast
{
    private static readonly TimeSpan ShortDuration = TimeSpan.FromSeconds(2);

    public static Task ShowSuccessAsync(string message) =>
        ShowAsync(message, Color.FromArgb("#2E7D32"));

    public static Task ShowErrorAsync(string message) =>
        ShowAsync(message, Color.FromArgb("#C62828"));

    public static Task ShowInfoAsync(string message) =>
        ShowAsync(message, Color.FromArgb("#E65100"));

    private static Task ShowAsync(string message, Color background)
    {
        var options = new SnackbarOptions
        {
            BackgroundColor = background,
            TextColor = Colors.White,
            CornerRadius = new CornerRadius(10),
        };

        var snackbar = Snackbar.Make(message, duration: ShortDuration, visualOptions: options);
        return snackbar.Show();
    }
}
