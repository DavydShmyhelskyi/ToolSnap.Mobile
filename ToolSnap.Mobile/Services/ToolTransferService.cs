using System.Net.Http.Json;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public record TransferResult(bool Success, ToolTransferDto? Transfer, string? ErrorMessage);

public class ToolTransferService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public ToolTransferService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TransferResult> InitiateAsync(
        Guid fromUserId,
        Guid toUserId,
        Guid toolId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "tool-transfers",
                new InitiateToolTransferDto(fromUserId, toUserId, toolId),
                cancellationToken);

            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new TransferResult(false, null, text);

            return new TransferResult(
                true,
                JsonSerializer.Deserialize<ToolTransferDto>(text, JsonOptions),
                null);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, null, ex.Message);
        }
    }

    public async Task<TransferResult> AcceptAsync(
        Guid transferId,
        Guid responderUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"tool-transfers/{transferId}/accept")
            {
                Content = JsonContent.Create(new RespondToToolTransferDto(responderUserId))
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new TransferResult(false, null, text);

            return new TransferResult(
                true,
                JsonSerializer.Deserialize<ToolTransferDto>(text, JsonOptions),
                null);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, null, ex.Message);
        }
    }

    public async Task<TransferResult> RejectAsync(
        Guid transferId,
        Guid responderUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"tool-transfers/{transferId}/reject")
            {
                Content = JsonContent.Create(new RespondToToolTransferDto(responderUserId))
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new TransferResult(false, null, text);

            return new TransferResult(
                true,
                JsonSerializer.Deserialize<ToolTransferDto>(text, JsonOptions),
                null);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, null, ex.Message);
        }
    }

    public async Task<TransferResult> CancelAsync(
        Guid transferId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"tool-transfers/{transferId}/cancel")
            {
                Content = JsonContent.Create(new CancelToolTransferDto(initiatorUserId))
            };

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new TransferResult(false, null, text);

            return new TransferResult(
                true,
                JsonSerializer.Deserialize<ToolTransferDto>(text, JsonOptions),
                null);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, null, ex.Message);
        }
    }

    public async Task<List<ToolTransferDto>> GetPendingIncomingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"tool-transfers/pending/to/{userId}",
                cancellationToken);

            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new List<ToolTransferDto>();

            return JsonSerializer.Deserialize<List<ToolTransferDto>>(text, JsonOptions)
                   ?? new List<ToolTransferDto>();
        }
        catch
        {
            return new List<ToolTransferDto>();
        }
    }

    public async Task<List<ToolTransferDto>> GetInitiatedByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"tool-transfers/initiated-by/{userId}",
                cancellationToken);

            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new List<ToolTransferDto>();

            return JsonSerializer.Deserialize<List<ToolTransferDto>>(text, JsonOptions)
                   ?? new List<ToolTransferDto>();
        }
        catch
        {
            return new List<ToolTransferDto>();
        }
    }

    public async Task<ToolDto?> GetToolByIdAsync(
        Guid toolId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"tools/{toolId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var text = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ToolDto>(text, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
