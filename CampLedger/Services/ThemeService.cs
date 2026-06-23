using CampLedger.Resources.Styles.Themes;
using Microsoft.Maui.Storage;

namespace CampLedger.Services;

public sealed class ThemeService : IThemeService
{
    private const string SelectedThemePreferenceKey = "SelectedThemeName";

    private static readonly string[] ThemeOptions =
    {
        "Regal Earth",
        "Wine Desert",
        "Forest Lavender"
    };

    public ThemeService()
        : this(Microsoft.Maui.Storage.Preferences.Default)
    {
    }

    public ThemeService(IPreferences preferences)
    {
        Preferences = preferences;
        CurrentThemeName = "Regal Earth";
    }

    private IPreferences Preferences { get; }

    public string CurrentThemeName { get; private set; }

    public IReadOnlyList<string> GetThemeOptions()
    {
        return ThemeOptions;
    }

    public void Initialize()
    {
        var savedThemeName = Preferences.Get(SelectedThemePreferenceKey, "Regal Earth");
        if (!IsValidThemeName(savedThemeName))
        {
            savedThemeName = "Regal Earth";
        }

        ApplyTheme(savedThemeName);
        CurrentThemeName = savedThemeName;
    }

    public bool SetTheme(string themeName)
    {
        if (!IsValidThemeName(themeName))
        {
            return false;
        }

        if (string.Equals(CurrentThemeName, themeName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ApplyTheme(themeName);
        CurrentThemeName = themeName;
        Preferences.Set(SelectedThemePreferenceKey, themeName);
        return true;
    }

    private static bool IsValidThemeName(string themeName)
    {
        for (var index = 0; index < ThemeOptions.Length; index++)
        {
            if (string.Equals(ThemeOptions[index], themeName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static ResourceDictionary CreateThemeDictionary(string themeName)
    {
        return themeName switch
        {
            "Wine Desert" => new WineDesertTheme(),
            "Forest Lavender" => new ForestLavenderTheme(),
            _ => new RegalEarthTheme()
        };
    }

    private static bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        if (dictionary is RegalEarthTheme || dictionary is WineDesertTheme || dictionary is ForestLavenderTheme)
        {
            return true;
        }

        string? source = dictionary.Source?.OriginalString;
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        return source.Contains("Resources/Styles/Themes/", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyTheme(string themeName)
    {
        Application? application = Application.Current;
        if (application?.Resources is null)
        {
            return;
        }

        ICollection<ResourceDictionary> mergedDictionaries = application.Resources.MergedDictionaries;
        if (mergedDictionaries is null)
        {
            return;
        }

        List<ResourceDictionary> existingThemes = mergedDictionaries.Where(IsThemeDictionary).ToList();

        mergedDictionaries.Add(CreateThemeDictionary(themeName));

        foreach (ResourceDictionary existingTheme in existingThemes)
        {
            mergedDictionaries.Remove(existingTheme);
        }
    }
}
