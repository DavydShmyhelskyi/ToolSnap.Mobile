using System.Collections.ObjectModel;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Models;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class IncomingTransfersPage : ContentPage
{
    private readonly ToolTransferService _transferService;
    private readonly UserSessionService _session;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public ObservableCollection<TransferListItem> IncomingItems { get; } = new();
    public ObservableCollection<TransferListItem> OutgoingItems { get; } = new();

    public IncomingTransfersPage(
        ToolTransferService transferService,
        UserSessionService session,
        HttpClient httpClient)
    {
        InitializeComponent();
        _transferService = transferService;
        _session = session;
        _httpClient = httpClient;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
        await AppShell.RefreshBadgeAsync(_transferService, _session);
    }

    private async Task RefreshAsync()
    {
        var user = _session.CurrentUser;
        if (user == null) return;

        try
        {
            // Kick off all independent network calls in parallel
            var usersTask = _httpClient.GetAsync("users");
            var toolTypesTask = _httpClient.GetAsync("tool-types");
            var incomingTask = _transferService.GetPendingIncomingAsync(user.Id);
            var outgoingTask = _transferService.GetInitiatedByUserAsync(user.Id);

            await Task.WhenAll(usersTask, toolTypesTask, incomingTask, outgoingTask);

            // Build user name dictionary
            var usersText = await (await usersTask).Content.ReadAsStringAsync();
            var userDict = (JsonSerializer.Deserialize<List<UserDto>>(usersText, JsonOptions) ?? new())
                .ToDictionary(u => u.Id, u => u.FullName);

            // Build tool-type name dictionary
            var ttText = await (await toolTypesTask).Content.ReadAsStringAsync();
            var ttDict = (JsonSerializer.Deserialize<List<ToolTypeDto>>(ttText, JsonOptions) ?? new())
                .ToDictionary(t => t.Id, t => t.Title);

            var incoming = await incomingTask;
            var outgoing = await outgoingTask;

            // Resolve tool info for each unique tool ID
            var uniqueToolIds = incoming.Select(t => t.ToolId)
                .Concat(outgoing.Select(t => t.ToolId))
                .Distinct()
                .ToList();

            var toolDict = new Dictionary<Guid, ToolDto>();
            foreach (var toolId in uniqueToolIds)
            {
                var tool = await _transferService.GetToolByIdAsync(toolId);
                if (tool != null)
                    toolDict[toolId] = tool;
            }

            string BuildToolLabel(Guid toolId)
            {
                if (!toolDict.TryGetValue(toolId, out var tool))
                    return $"Tool #{toolId.ToString()[..8]}";

                var typeName = ttDict.TryGetValue(tool.ToolTypeId, out var tt) ? tt : "Tool";
                return string.IsNullOrWhiteSpace(tool.SerialNumber)
                    ? typeName
                    : $"{typeName} — {tool.SerialNumber}";
            }

            string ResolveUser(Guid userId) =>
                userDict.TryGetValue(userId, out var name)
                    ? name
                    : $"User #{userId.ToString()[..8]}";

            // Populate incoming list
            IncomingItems.Clear();
            foreach (var t in incoming)
            {
                var item = new TransferListItem
                {
                    TransferId = t.Id,
                    ToolId = t.ToolId,
                    ToolLabel = BuildToolLabel(t.ToolId),
                    OtherUserName = ResolveUser(t.FromUserId),
                    Status = t.Status,
                    InitiatedAtText = t.InitiatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    CanAccept = true,
                    CanReject = true,
                    CanCancel = false
                };
                item.AcceptCommand = new Command(async () => await AcceptAsync(item));
                item.RejectCommand = new Command(async () => await RejectAsync(item));
                IncomingItems.Add(item);
            }

            // Populate outgoing list
            OutgoingItems.Clear();
            foreach (var t in outgoing)
            {
                var item = new TransferListItem
                {
                    TransferId = t.Id,
                    ToolId = t.ToolId,
                    ToolLabel = BuildToolLabel(t.ToolId),
                    OtherUserName = ResolveUser(t.ToUserId),
                    Status = t.Status,
                    InitiatedAtText = t.InitiatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    CanAccept = false,
                    CanReject = false,
                    CanCancel = t.Status == "Pending"
                };
                item.CancelCommand = new Command(async () => await CancelAsync(item));
                OutgoingItems.Add(item);
            }

            NoIncomingLabel.IsVisible = IncomingItems.Count == 0;
            NoOutgoingLabel.IsVisible = OutgoingItems.Count == 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async Task AcceptAsync(TransferListItem item)
    {
        var user = _session.CurrentUser;
        if (user == null) return;

        var result = await _transferService.AcceptAsync(item.TransferId, user.Id);

        if (!result.Success)
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Failed to accept.", "OK");
            return;
        }

        await DisplayAlert("Done", "Transfer accepted. The tool is now assigned to you.", "OK");
        await RefreshAsync();
        await AppShell.RefreshBadgeAsync(_transferService, _session);
    }

    private async Task RejectAsync(TransferListItem item)
    {
        var user = _session.CurrentUser;
        if (user == null) return;

        var result = await _transferService.RejectAsync(item.TransferId, user.Id);

        if (!result.Success)
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Failed to reject.", "OK");
            return;
        }

        await DisplayAlert("Done", "Transfer rejected.", "OK");
        await RefreshAsync();
        await AppShell.RefreshBadgeAsync(_transferService, _session);
    }

    private async Task CancelAsync(TransferListItem item)
    {
        var user = _session.CurrentUser;
        if (user == null) return;

        var result = await _transferService.CancelAsync(item.TransferId, user.Id);

        if (!result.Success)
        {
            await DisplayAlert("Error", result.ErrorMessage ?? "Failed to cancel.", "OK");
            return;
        }

        await DisplayAlert("Done", "Transfer cancelled.", "OK");
        await RefreshAsync();
    }
}
