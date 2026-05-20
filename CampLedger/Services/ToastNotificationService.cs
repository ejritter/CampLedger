using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace CampLedger.Services;

public sealed class ToastNotificationService : IToastNotificationService
{
    public async Task ShowAsync(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var snackbar = Snackbar.Make(message, duration: TimeSpan.FromSeconds(3));
            await snackbar.Show();
        });
    }
}
