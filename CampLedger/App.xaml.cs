using CampLedger.Services;

namespace CampLedger;

public partial class App : Application
{
    private readonly IThemeService _themeService;
    private readonly AppShell _appShell;
    private Window? _mainWindow;

    public App(IThemeService themeService, AppShell appShell)
    {
        InitializeComponent();
        _themeService = themeService;
        _themeService.Initialize();
        _appShell = appShell;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _mainWindow = new Window(_appShell);
        return _mainWindow;
    }
}
