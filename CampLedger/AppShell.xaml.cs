using CampLedger.Pages;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CampLedger;

public partial class AppShell : Shell
{
    private readonly IServiceProvider _serviceProvider;
    private const double FlyoutWidthRatio = 0.05;
    private const double PhoneFlyoutWidth = 80d;

    public AppShell(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            FlyoutWidth = PhoneFlyoutWidth;
        else
             FlyoutWidth = Width * FlyoutWidthRatio;
        InitializeComponent();
    }

    private void OnShellSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0)
        {
            return;
        }

        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            FlyoutWidth = PhoneFlyoutWidth;
            return;
        }

        FlyoutWidth = Width * FlyoutWidthRatio;
    }

    private async void OnThemeGearTapped(object sender, TappedEventArgs e)
    {
        ContentPage? currentPage = Shell.Current?.CurrentPage as ContentPage ?? Application.Current?.MainPage as ContentPage;
        if (currentPage is null)
        {
            return;
        }

        ThemeSelectionPopup popup = _serviceProvider.GetRequiredService<ThemeSelectionPopup>();
        await PopupExtensions.ShowPopupAsync(currentPage, popup);
    }
}
