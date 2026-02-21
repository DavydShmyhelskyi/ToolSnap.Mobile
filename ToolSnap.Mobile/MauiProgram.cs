using Microsoft.Extensions.Logging;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<AuthTokenService>();

            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var tokenService = sp.GetRequiredService<AuthTokenService>();
                var handler = new AuthenticatedHttpClientHandler(tokenService);

                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://localhost:7062/")
                };
            });

            builder.Services.AddSingleton<UserSessionService>();
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<ToolTakeService>();
            builder.Services.AddSingleton<DetectionParsingService>();
            builder.Services.AddSingleton<DetectedToolsService>();
            builder.Services.AddSingleton<ToolConfirmationService>();
            builder.Services.AddSingleton<TakeFlowStateService>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
