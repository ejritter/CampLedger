using System.Text;
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
        try
        {
            InitializeComponent();
            UpdateFlyoutWidth();
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "AppShell initialization");
            throw;
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateFlyoutWidth(width);
    }

    private void OnShellSizeChanged(object? sender, EventArgs e)
    {
        UpdateFlyoutWidth();
    }

    private void UpdateFlyoutWidth(double? width = null)
    {
        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
        {
            FlyoutWidth = PhoneFlyoutWidth;
            return;
        }

        var availableWidth = width ?? Width;
        if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth <= 0)
        {
            availableWidth = 1024d;
        }

        FlyoutWidth = Math.Max(72d, Math.Min(180d, availableWidth * FlyoutWidthRatio));
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

    private static void LogStartupFailure(Exception ex, string stage)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CampLedger");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "startup-errors.log");
            var builder = new StringBuilder();
            builder.AppendLine($"{DateTimeOffset.Now:O}");
            builder.AppendLine($"Stage: {stage}");
            builder.AppendLine(ex.GetType().FullName);
            builder.AppendLine(ex.Message);
            builder.AppendLine(ex.StackTrace);
            File.AppendAllText(path, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
        }
    }
}
