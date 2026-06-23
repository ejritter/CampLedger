using CampLedger.Resources.Styles;
using CampLedger.Services;
using CampLedger.Tests.TestDoubles;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CampLedger.Tests;

public sealed class ThemeServiceTests
{
    [Fact]
    public void Initialize_WithSavedTheme_AppliesStoredThemeToActiveResources()
    {
        var preferences = new InMemoryPreferences();
        preferences.Set("SelectedThemeName", "Wine Desert", null);
        preferences.Set("LastThemeChangeTicks", 123L, null);
        var sut = CreateSystemUnderTest(preferences);

        sut.Initialize();

        Assert.Equal("Wine Desert", sut.CurrentThemeName);
        Assert.NotNull(sut.LastThemeChangedUtc);
        Assert.Equal(new DateTimeOffset(123L, TimeSpan.Zero), sut.LastThemeChangedUtc);
        Assert.Equal(Color.FromArgb("#6B1E36"), Application.Current!.Resources["themeBrandColor"]);
        Assert.Equal(Color.FromArgb("#F6E7D2"), Application.Current!.Resources["themeBackgroundColor"]);
    }

    [Fact]
    public void SetTheme_WithValidTheme_PersistsThemeAndRaisesChangeEvent()
    {
        var preferences = new InMemoryPreferences();
        var sut = CreateSystemUnderTest(preferences);
        ThemeChangedEventArgs? eventArgs = null;
        sut.ThemeChanged += (_, args) => eventArgs = args;

        sut.Initialize();
        var changed = sut.SetTheme("Forest Lavender");

        Assert.True(changed);
        Assert.Equal("Forest Lavender", sut.CurrentThemeName);
        Assert.Equal("Forest Lavender", preferences.Get("SelectedThemeName", string.Empty, null));
        Assert.NotNull(sut.LastThemeChangedUtc);
        Assert.NotNull(eventArgs);
        Assert.Equal("Regal Earth", eventArgs!.PreviousThemeName);
        Assert.Equal("Forest Lavender", eventArgs.CurrentThemeName);
        Assert.Equal(Color.FromArgb("#184D37"), Application.Current!.Resources["themeBrandColor"]);
    }

    [Fact]
    public void SetTheme_WithInvalidTheme_ReturnsFalseAndLeavesCurrentThemeUnchanged()
    {
        var preferences = new InMemoryPreferences();
        var sut = CreateSystemUnderTest(preferences);

        sut.Initialize();
        var changed = sut.SetTheme("Not A Real Theme");

        Assert.False(changed);
        Assert.Equal("Regal Earth", sut.CurrentThemeName);
        Assert.Equal(string.Empty, preferences.Get("SelectedThemeName", string.Empty, null));
    }

    private static ThemeService CreateSystemUnderTest(IPreferences preferences)
    {
        EnsureApplicationResources();
        return new ThemeService(preferences);
    }

    private static void EnsureApplicationResources()
    {
        if (Application.Current is null)
        {
            _ = new Application();
        }

        var resources = Application.Current!.Resources;
        if (resources is null)
        {
            resources = new ResourceDictionary();
            Application.Current.Resources = resources;
        }

        resources.MergedDictionaries.Clear();
        resources.MergedDictionaries.Add(new CampLedger.Resources.Styles.Colors());
    }
}
