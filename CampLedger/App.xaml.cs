using System.Diagnostics;
using System.Text;
using CampLedger.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace CampLedger;

public partial class App : Application
{
    private const string StartupErrorLogFileName = "startup-errors.log";
    private readonly IThemeService _themeService;
    private readonly IServiceProvider? _serviceProvider;

    public App()
    {
        InitializeComponent();
        _serviceProvider = IPlatformApplication.Current?.Services;
        _themeService = _serviceProvider?.GetService<IThemeService>() ?? new ThemeService();
        _themeService.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            var shell = (_serviceProvider ?? ServiceHelper.Services).GetRequiredService<AppShell>();
            return new Window(shell);
        }
        catch (Exception ex)
        {
            var errorMessage = BuildStartupErrorMessage(ex);
            Debug.WriteLine(errorMessage);
            File.WriteAllText(GetStartupErrorLogPath(), errorMessage, Encoding.UTF8);
            var errorPage = CreateErrorPage(errorMessage);
            return new Window(errorPage);
        }
    }

    private static Page CreateErrorPage(string errorMessage)
    {
        return new ContentPage
        {
            Title = "Startup Error",
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(24),
                    Spacing = 16,
                    Children =
                    {
                        new Label { Text = "CampLedger could not start correctly.", FontAttributes = FontAttributes.Bold, FontSize = 20 },
                        new Label { Text = errorMessage, LineBreakMode = LineBreakMode.WordWrap },
                        new Label { Text = "Please check the startup-errors.log file in the app data directory for more details.", FontAttributes = FontAttributes.Italic }
                    }
                }
            }
        };
    }

    private static string BuildStartupErrorMessage(Exception ex)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{DateTimeOffset.Now:O}");
        builder.AppendLine(ex.GetType().FullName);
        builder.AppendLine(ex.Message);
        builder.AppendLine(ex.StackTrace);

        if (ex.InnerException is not null)
        {
            builder.AppendLine("Inner exception:");
            builder.AppendLine(ex.InnerException.GetType().FullName);
            builder.AppendLine(ex.InnerException.Message);
            builder.AppendLine(ex.InnerException.StackTrace);
        }

        return builder.ToString();
    }

    private static string GetStartupErrorLogPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, StartupErrorLogFileName);
    }
}
