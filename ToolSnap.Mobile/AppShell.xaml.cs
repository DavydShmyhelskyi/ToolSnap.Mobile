using Microsoft.Extensions.DependencyInjection;
using ToolSnap.Mobile.Pages;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile;

public partial class AppShell : Shell
{
    // ──────────────────────────────────────────────
    // Badge state — binds to the FlyoutHeader view
    // ──────────────────────────────────────────────

    private int _pendingTransfersCount;

    public int PendingTransfersCount
    {
        get => _pendingTransfersCount;
        private set
        {
            if (_pendingTransfersCount == value) return;
            _pendingTransfersCount = value;
            OnPropertyChanged(nameof(PendingTransfersCount));
            OnPropertyChanged(nameof(HasPendingTransfers));
            OnPropertyChanged(nameof(PendingTransfersBadgeText));
        }
    }

    public bool HasPendingTransfers => _pendingTransfersCount > 0;

    public string PendingTransfersBadgeText =>
        $"\U0001f514 {_pendingTransfersCount} pending transfer{(_pendingTransfersCount == 1 ? "" : "s")}";

    // ──────────────────────────────────────────────
    // Construction & route registration
    // ──────────────────────────────────────────────

    public AppShell()
    {
        InitializeComponent();

        // Bind the flyout header to this Shell so badge properties update it
        FlyoutHeaderLayout.BindingContext = this;

        Routing.RegisterRoute(nameof(TakePage), typeof(TakePage));
        Routing.RegisterRoute(nameof(ConfirmOnTakeToolAsignmentPage), typeof(ConfirmOnTakeToolAsignmentPage));
        Routing.RegisterRoute("profile1", typeof(ProfilePage1));
        Routing.RegisterRoute(nameof(ReturnPage), typeof(ReturnPage));
        Routing.RegisterRoute(nameof(ConfirmOnReturnToolAsignmentPage), typeof(ConfirmOnReturnToolAsignmentPage));
        Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
        Routing.RegisterRoute(nameof(TransferPage), typeof(TransferPage));

        Navigated += OnNavigated;
    }

    // ──────────────────────────────────────────────
    // Auto-refresh badge on every navigation event
    // ──────────────────────────────────────────────

    private async void OnNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        await RefreshBadgeCountAsync();
    }

    private async Task RefreshBadgeCountAsync()
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        if (services == null) return;

        var session = services.GetService<UserSessionService>();
        var transferService = services.GetService<ToolTransferService>();

        if (session?.CurrentUser == null || transferService == null) return;

        var pending = await transferService.GetPendingIncomingAsync(session.CurrentUser.Id);
        PendingTransfersCount = pending.Count;
    }

    // Called from pages after any transfer action that may change the incoming count
    public static Task RefreshBadgeAsync(ToolTransferService transferService, UserSessionService session)
    {
        if (Current is AppShell shell)
            return shell.RefreshBadgeCountAsync();

        return Task.CompletedTask;
    }
}
