using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace ToolSnap.Mobile.Services;

public class BiometricService
{
    private const string EnabledKey  = "biometric_enabled";
    private const string EmailKey    = "biometric_email";
    private const string PasswordKey = "biometric_password";

    public bool IsEnabled => Preferences.Get(EnabledKey, false);

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            return availability == FingerprintAvailability.Available;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AuthenticateAsync()
    {
        var config = new AuthenticationRequestConfiguration(
            "ToolSnap",
            "Sign in to ToolSnap");
        try
        {
            var result = await CrossFingerprint.Current.AuthenticateAsync(config);
            return result.Authenticated;
        }
        catch
        {
            return false;
        }
    }

    public async Task EnableAsync(string email, string password)
    {
        await SecureStorage.SetAsync(EmailKey, email);
        await SecureStorage.SetAsync(PasswordKey, password);
        Preferences.Set(EnabledKey, true);
    }

    public void Disable()
    {
        Preferences.Remove(EnabledKey);
        SecureStorage.Remove(EmailKey);
        SecureStorage.Remove(PasswordKey);
    }

    public async Task<(string? Email, string? Password)> GetCredentialsAsync()
    {
        var email    = await SecureStorage.GetAsync(EmailKey);
        var password = await SecureStorage.GetAsync(PasswordKey);
        return (email, password);
    }
}
