using Microsoft.Extensions.Logging;
using TrackPointV.Service;
using TrackPointV.View.DBView;
using TrackPointV.View;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace TrackPointV
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
                    fonts.AddFont("fa-solid-900.ttf", "Font Awesome 6 Free Solid");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    // Register ZXing.Net.MAUI handlers
                    handlers.AddHandler(typeof(CameraBarcodeReaderView), typeof(CameraBarcodeReaderViewHandler));
                    handlers.AddHandler(typeof(BarcodeGeneratorView), typeof(BarcodeGeneratorViewHandler));
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Register services
            builder.Services.AddSingleton<IUserAuthentication, UserAuthentication>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<GoogleSignInPage>();

            // Register Google authentication service
            builder.Services.AddSingleton(new GoogleAuthWindowsService(
                "16120070774-tjfv13ssju0f6e1ftrqk2av06713g9q1.apps.googleusercontent.com",
                "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"));
            
            return builder.Build();
        }
    }
}
