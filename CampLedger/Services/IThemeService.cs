namespace CampLedger.Services;

public interface IThemeService
{
    string CurrentThemeName { get; }

    IReadOnlyList<string> GetThemeOptions();

    void Initialize();

    bool SetTheme(string themeName);
}
