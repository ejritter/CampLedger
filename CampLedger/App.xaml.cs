using System.Diagnostics;
using System.Text;
using CampLedger.Pages;
using CampLedger.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;

namespace CampLedger;

public partial class App : Application
{
    private const string StartupErrorLogFileName = "startup-errors.log";
    private const string StartupTraceLogFileName = "startup-trace.log";
    private readonly IThemeService _themeService;
    private readonly IServiceProvider? _serviceProvider;
    private bool _themeInitialized;

    public App()
    {
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        LogStartupTrace("App.ctor", "starting");
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
            LogStartupTrace("App.ctor", $"services={_serviceProvider is not null}");
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
            LogStartupTrace("CreateWindow", "begin");
            InitializeThemeIfNeeded();
            var rootPage = CreateRootPage();
            LogStartupTrace("CreateWindow", $"rootPage={rootPage.GetType().FullName}");
            var window = new Window(rootPage);
            window.Created += OnWindowCreated;
            LogStartupTrace("CreateWindow", $"windows={Application.Current?.Windows.Count ?? 0}");
            LogStartupTrace("CreateWindow", $"handlerSet={window.Handler is not null}");
            LogStartupTrace("CreateWindow", "created window");
            return window;
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "CreateWindow shell startup");
            LogStartupTrace("CreateWindow", $"failed: {ex.Message}");
            return new Window(new ContentPage
            {
                Content = new Label { Text = ex.Message }
            });
        }
    }

    private Page CreateRootPage()
    {
        try
        {
            if (_serviceProvider is null)
            {
                return new ContentPage
                {
                    Title = "CampLedger",
                    Content = new Label { Text = "CampLedger is starting..." }
                };
            }

            return _serviceProvider.GetRequiredService<CampLedgerNavigationPage>();
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "CreateRootPage");
            return new ContentPage
            {
                Title = "CampLedger",
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(24),
                    Spacing = 12,
                    Children =
                    {
                        new Label { Text = "CampLedger is starting...", FontAttributes = FontAttributes.Bold, FontSize = 20 },
                        new Label { Text = ex.Message, LineBreakMode = LineBreakMode.WordWrap }
                    }
                }
            };
        }
    }

    private void OnWindowCreated(object? sender, EventArgs e)
    {
        LogStartupTrace("OnWindowCreated", "begin");
        if (sender is not Window window || window.Page is null)
        {
            LogStartupTrace("OnWindowCreated", "window or page missing");
            return;
        }

        LogStartupTrace("OnWindowCreated", $"page={window.Page.GetType().FullName}");
        window.Page.Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        LogStartupTrace("OnPageLoaded", "begin");

        // Safety net only: InitializeThemeIfNeeded() already ran synchronously in
        // CreateWindow(), before the root page (and its nav bar chrome) was ever
        // constructed. This call is guarded by _themeInitialized and is expected
        // to be a no-op in the normal startup path.
        InitializeThemeIfNeeded();
    }

    private void InitializeThemeIfNeeded()
    {
        if (_themeInitialized)
        {
            return;
        }

        _themeInitialized = true;

        try
        {
            // Must run before CreateRootPage() resolves CampLedgerNavigationPage:
            // that page reads Application.Current.Resources synchronously while
            // building its header/footer/nav items, so the saved theme dictionary
            // needs to already be merged in or every color falls back to
            // CampLedgerNavigationPage's hardcoded defaults until the user opens
            // the theme popup at least once. ThemeService.Initialize() is fully
            // synchronous (Preferences read + ResourceDictionary swap only), so
            // calling it directly here carries no sync-over-async deadlock risk.
            LogStartupTrace("InitializeThemeIfNeeded", "initializing theme");
            _themeService?.Initialize();
        }
        catch (Exception ex)
        {
            LogStartupFailure(ex, "Theme initialization before root page creation");
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

    private static void LogStartupTrace(string stage, string message)
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CampLedger", StartupTraceLogFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTimeOffset.Now:O} [{stage}] {message}{Environment.NewLine}", Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
    {
        try
        {
            if (e.Exception is null)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine($"{e.Exception.GetType().FullName}: {e.Exception.Message}");
            if (!string.IsNullOrWhiteSpace(e.Exception.StackTrace))
            {
                builder.AppendLine(e.Exception.StackTrace);
            }

            LogStartupTrace("FirstChanceException", builder.ToString());
        }
        catch
        {
        }
    }
}
