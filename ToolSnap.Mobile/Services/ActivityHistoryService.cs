using System.Net.Http.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public record ActivityItem(
    string OperationLabel,
    string OperationColor,
    string ToolDisplay,
    string Timestamp,
    string? StatusBadge,
    DateTime SortKey)
{
    public bool HasStatus => StatusBadge is not null;
}

public class ActivityHistoryService(HttpClient httpClient, UserSessionService session)
{
    public async Task<IReadOnlyList<ActivityItem>> LoadAsync()
    {
        var userId = session.CurrentUser?.Id;
        if (userId is null)
            return [];

        var id = userId.Value;

        var assignmentsTask  = httpClient.GetFromJsonAsync<List<ToolAssignmentDto>>($"tool-assignments/user/{id}");
        var sentTask         = httpClient.GetFromJsonAsync<List<ToolTransferDto>>($"tool-transfers/initiated-by/{id}");
        var receivedTask     = httpClient.GetFromJsonAsync<List<ToolTransferDto>>($"tool-transfers/received-by/{id}");

        await Task.WhenAll(assignmentsTask, sentTask, receivedTask);

        var assignments = assignmentsTask.Result ?? [];
        var sent        = sentTask.Result        ?? [];
        var received    = receivedTask.Result    ?? [];

        var toolIds = assignments.Select(a => a.ToolId)
            .Concat(sent.Select(t => t.ToolId))
            .Concat(received.Select(t => t.ToolId))
            .Distinct()
            .ToList();

        var toolMap = await FetchToolsAsync(toolIds);

        var items = new List<ActivityItem>();

        foreach (var a in assignments)
        {
            var display = ToolDisplay(a.ToolId, toolMap);

            items.Add(new ActivityItem(
                "Took tool",
                "#E65100",
                display,
                FormatDate(a.TakenAt),
                null,
                a.TakenAt));

            if (a.ReturnedAt.HasValue)
            {
                items.Add(new ActivityItem(
                    "Returned tool",
                    "#43A047",
                    display,
                    FormatDate(a.ReturnedAt.Value),
                    null,
                    a.ReturnedAt.Value));
            }
        }

        foreach (var t in sent)
        {
            var display = ToolDisplay(t.ToolId, toolMap);
            items.Add(new ActivityItem(
                "Transfer sent",
                "#1976D2",
                display,
                FormatDate(t.InitiatedAt),
                t.Status,
                t.InitiatedAt));
        }

        foreach (var t in received)
        {
            var display = ToolDisplay(t.ToolId, toolMap);
            var eventTime = t.RespondedAt ?? t.InitiatedAt;
            items.Add(new ActivityItem(
                "Transfer received",
                "#AB47BC",
                display,
                FormatDate(eventTime),
                t.Status,
                eventTime));
        }

        return items
            .OrderByDescending(i => i.SortKey)
            .ToList();
    }

    private async Task<Dictionary<Guid, ToolDto>> FetchToolsAsync(IEnumerable<Guid> toolIds)
    {
        var tasks = toolIds.Select(async id =>
        {
            try
            {
                var tool = await httpClient.GetFromJsonAsync<ToolDto>($"tools/{id}");
                return (id: id, tool: tool);
            }
            catch
            {
                return (id: id, tool: (ToolDto?)null);
            }
        });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.tool is not null)
            .ToDictionary(r => r.id, r => r.tool!);
    }

    private static string ToolDisplay(Guid toolId, Dictionary<Guid, ToolDto> toolMap)
    {
        if (toolMap.TryGetValue(toolId, out var tool) && !string.IsNullOrEmpty(tool.SerialNumber))
            return $"S/N: {tool.SerialNumber}";

        return $"Tool #{toolId.ToString()[..8]}";
    }

    private static string FormatDate(DateTime dt) =>
        dt.ToLocalTime().ToString("dd MMM yyyy  HH:mm");
}
