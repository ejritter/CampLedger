using CampLedger.Services;
using CommunityToolkit.Mvvm.Input;

namespace CampLedger.ViewModels;

public sealed partial class ThemeSelectionPopupViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;

    public ThemeSelectionPopupViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        CurrentThemeName = themeService.CurrentThemeName;
    }

    public string CurrentThemeName { get; }

    public event EventHandler? CloseRequested;

    [RelayCommand]
    private void SelectTheme(string themeName)
    {
        _themeService.SetTheme(themeName);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
