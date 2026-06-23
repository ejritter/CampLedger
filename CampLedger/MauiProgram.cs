using System.Text;
using CampLedger.Services;
using CampLedger.ViewModels;
using CampLedger.Pages;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;

namespace CampLedger;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        try
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(true);
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Segoe-Fluent-Icons.ttf", "Segoe Fluent Icons");
                });

            builder.Services.AddSingleton<ICampLedgerStorageService, CampLedgerStorageService>();
            builder.Services.AddSingleton<ICampLedgerStateService, CampLedgerStateService>();
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddSingleton<IToastNotificationService, ToastNotificationService>();
            builder.Services.AddSingleton<ITripDurationValidationService, TripDurationValidationService>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<ThemeSelectionPopupViewModel>();
            builder.Services.AddTransientPopup<ThemeSelectionPopup>();
            builder.Services.AddTransient<InventoryViewModel>();
            builder.Services.AddTransient<InventoryPage>();
            builder.Services.AddTransient<TripLedgerViewModel>();
            builder.Services.AddTransient<TripLedgerPage>();
            builder.Services.AddTransient<TripLocationPopupPage>();
            builder.Services.AddTransient<TripHistoryViewModel>();
            builder.Services.AddTransient<TripHistoryPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            ServiceHelper.Services = app.Services;
            return app;
        }
        catch (Exception ex)
        {
            WriteStartupLog(ex, "MauiProgram.CreateMauiApp");
            throw;
        }
    }

    private static void WriteStartupLog(Exception ex, string stage)
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CampLedger", "startup-errors.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var builder = new StringBuilder();
            builder.AppendLine($"{DateTimeOffset.Now:O}");
            builder.AppendLine($"Stage: {stage}");
            builder.AppendLine(ex.GetType().FullName);
            builder.AppendLine(ex.Message);
            builder.AppendLine(ex.StackTrace);
            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
        }
    }
}
