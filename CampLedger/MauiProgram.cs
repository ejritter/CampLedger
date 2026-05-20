using CampLedger.Services;
using CampLedger.ViewModels;
using CampLedger.Pages;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace CampLedger;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
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
            });

        builder.Services.AddSingleton<ICampLedgerStorageService, CampLedgerStorageService>();
        builder.Services.AddSingleton<ICampLedgerStateService, CampLedgerStateService>();
        builder.Services.AddSingleton<IToastNotificationService, ToastNotificationService>();
        builder.Services.AddSingleton<InventoryViewModel>();
        builder.Services.AddSingleton<InventoryPage>();
        builder.Services.AddSingleton<TripLedgerViewModel>();
        builder.Services.AddSingleton<TripLedgerPage>();
        builder.Services.AddSingleton<TripLocationPopupPage>();
        builder.Services.AddSingleton<TripHistoryViewModel>();
        builder.Services.AddSingleton<TripHistoryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceHelper.Services = app.Services;
        return app;
    }
}
