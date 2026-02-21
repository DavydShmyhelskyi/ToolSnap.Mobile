using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly AuthTokenService _tokenService;
    private bool _isRefreshing;

    public AuthenticatedHttpClientHandler(AuthTokenService tokenService)
    {
        _tokenService = tokenService;
        InnerHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Don't add token to auth endpoints
        if (request.RequestUri?.AbsolutePath.Contains("/auth/") == true &&
            !request.RequestUri.AbsolutePath.Contains("/auth/profile"))
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var accessToken = await _tokenService.GetAccessTokenAsync();

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Try to refresh token if unauthorized
        if (response.StatusCode == HttpStatusCode.Unauthorized && !_isRefreshing)
        {
            _isRefreshing = true;

            try
            {
                var refreshToken = await _tokenService.GetRefreshTokenAsync();

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshed = await RefreshTokenAsync(request.RequestUri!.GetLeftPart(UriPartial.Authority), refreshToken, cancellationToken);

                    if (refreshed)
                    {
                        // Retry original request with new token
                        var newAccessToken = await _tokenService.GetAccessTokenAsync();
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        return response;
    }

    private async Task<bool> RefreshTokenAsync(string baseUrl, string refreshToken, CancellationToken cancellationToken)
    {
        try
        {
            using var refreshClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            })
            {
                BaseAddress = new Uri(baseUrl)
            };

            var refreshRequest = new RefreshTokenDto(refreshToken);
            var response = await refreshClient.PostAsJsonAsync("auth/refresh", refreshRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthenticationResponseDto>(cancellationToken);

                if (authResponse != null)
                {
                    await _tokenService.SetTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
                    return true;
                }
            }
        }
        catch
        {
            // Refresh failed - clear tokens
            _tokenService.ClearTokens();
        }

        return false;
    }
}
