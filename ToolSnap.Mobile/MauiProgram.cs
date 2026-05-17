using Microsoft.Extensions.Logging;
using ToolSnap.Mobile.Pages;
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
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;

            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://10.0.2.2:7062/")
                };
            });
            builder.Services.AddTransient<ProfilePage1>();

            builder.Services.AddTransient<FindToolsForMapService>();
            builder.Services.AddSingleton<UserSessionService>();
            builder.Services.AddSingleton<LocationService>();
            builder.Services.AddSingleton<ToolTakeService>();
            builder.Services.AddSingleton<DetectionParsingService>();
            builder.Services.AddSingleton<DetectedToolsService>();
            builder.Services.AddSingleton<ToolConfirmationService>();
            builder.Services.AddSingleton<TakeFlowStateService>();
            builder.Services.AddSingleton<ToolTransferService>();
            builder.Services.AddSingleton<TransferFlowStateService>();
            builder.Services.AddSingleton<FcmTokenService>();

            builder.Services.AddTransient<TransferPage>();
            builder.Services.AddTransient<IncomingTransfersPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
