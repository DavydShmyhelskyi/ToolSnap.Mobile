using System.Net.Http.Json;

namespace ToolSnap.Mobile.Services;

// Stores and registers FCM push notification tokens with the backend.
//
// To receive actual push notifications:
//   Android — add google-services.json to Platforms/Android/ and call
//             StoreToken() from your FirebaseMessagingService.OnNewToken() override.
//   iOS     — add GoogleService-Info.plist to Platforms/iOS/ and call
//             StoreToken() from UNUserNotificationCenter registration callback.
//
// The backend's PATCH /users/{id}/fcm-token endpoint stores the token so the
// DeadlineCheckerJob can send push notifications 1 hour before tool due time.
public class FcmTokenService
{
    private const string FcmTokenKey = "fcm_token";
    private readonly HttpClient _httpClient;

    public FcmTokenService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // Called by platform-specific code when a new FCM token is issued.
    public void StoreToken(string token) =>
        Preferences.Set(FcmTokenKey, token);

    // Called after login — reads the stored token and registers it with the backend.
    // Returns silently if no token is stored yet (first install before Firebase init).
    public async Task RegisterAsync(Guid userId)
    {
        var token = Preferences.Get(FcmTokenKey, null);
        if (string.IsNullOrEmpty(token))
            return;

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"users/{userId}/fcm-token")
            {
                Content = JsonContent.Create(new { FcmToken = token })
            };
            await _httpClient.SendAsync(request);
        }
        catch
        {
            // Non-fatal — registration will succeed on the next login.
        }
    }
}
