using System.Diagnostics;
using System.Text;
using CampLedger.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;

namespace CampLedger;

public partial class App : Application
{
    private const string StartupErrorLogFileName = "startup-errors.log";
    private readonly IThemeService _themeService;
    private readonly IServiceProvider? _serviceProvider;
    private bool _themeInitialized;

    public App()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "InitializeComponent");
            throw;
        }

        try
        {
            _serviceProvider = IPlatformApplication.Current?.Services;
            _themeService = _serviceProvider?.GetService<IThemeService>() ?? new ThemeService();

            var services = _serviceProvider ?? new ServiceCollection().BuildServiceProvider();
            MainPage = services.GetService<AppShell>() ?? new AppShell(services);
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "App constructor initialization");
            throw;
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try
        {
            var window = new Window(MainPage ?? new ContentPage
            {
                Content = new Label { Text = "CampLedger is starting..." }
            });
            window.Created += OnWindowCreated;
            return window;
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "CreateWindow shell startup");
            return new Window(new ContentPage
            {
                Content = new Label { Text = ex.Message }
            });
        }
    }

    private void OnWindowCreated(object? sender, EventArgs e)
    {
        if (sender is not Window window || window.Page is null)
        {
            return;
        }

        window.Page.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        if (_themeInitialized)
        {
            return;
        }

        _themeInitialized = true;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                _themeService?.Initialize();
            }
            catch (Exception ex)
            {
                LogStartupFailure(ex, "Theme initialization after page load");
            }
        });
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

    private static void LogStartupFailure(Exception ex, string stage)
    {
        try
        {
            var message = BuildStartupErrorMessage(ex, stage);
            File.WriteAllText(GetStartupErrorLogPath(), message, Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static string BuildStartupErrorMessage(Exception ex, string? stage = null)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{DateTimeOffset.Now:O}");
        if (!string.IsNullOrWhiteSpace(stage))
        {
            builder.AppendLine($"Stage: {stage}");
        }

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
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(localAppData, "CampLedger");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, StartupErrorLogFileName);
    }
}
