using CampLedger.Services;

namespace CampLedger.Tests.TestDoubles;

public sealed class RecordingThemeService : IThemeService
{
    private readonly IReadOnlyList<string> _themeOptions = new[] { "Regal Earth", "Wine Desert", "Forest Lavender" };

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public string CurrentThemeName { get; private set; } = "Regal Earth";

    public DateTimeOffset? LastThemeChangedUtc { get; private set; }

    public string? LastRequestedThemeName { get; private set; }

    public int SetThemeCallCount { get; private set; }

    public IReadOnlyList<string> GetThemeOptions()
    {
        return _themeOptions;
    }

    public void Initialize()
    {
    }

    public bool SetTheme(string themeName)
    {
        SetThemeCallCount++;
        LastRequestedThemeName = themeName;

        if (string.IsNullOrWhiteSpace(themeName))
        {
            return false;
        }

        if (string.Equals(CurrentThemeName, themeName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var previousThemeName = CurrentThemeName;
        CurrentThemeName = themeName;
        LastThemeChangedUtc = DateTimeOffset.UtcNow;
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previousThemeName, CurrentThemeName, LastThemeChangedUtc.Value));
        return true;
    }
}
