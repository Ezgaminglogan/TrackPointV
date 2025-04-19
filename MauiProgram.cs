using Microsoft.Extensions.Logging;
using TrackPointV.Service;
using TrackPointV.View.DBView;

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
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Add this to your builder.Services section
            builder.Services.AddSingleton<IUserAuthentication, UserAuthentication>();
            builder.Services.AddTransient<DashboardPage>();
            return builder.Build();
        }
    }
}
