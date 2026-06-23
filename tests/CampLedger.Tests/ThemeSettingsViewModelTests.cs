using CampLedger.Services;
using CampLedger.Tests.TestDoubles;
using CampLedger.ViewModels;

namespace CampLedger.Tests;

public sealed class ThemeSettingsViewModelTests
{
    [Fact]
    public void Constructor_InitializesThemeOptionsAndSelectionFromService()
    {
        var themeService = new RecordingThemeService();

        var sut = new ThemeSettingsViewModel(themeService);

        Assert.Equal("Regal Earth", sut.CurrentThemeName);
        Assert.Equal("Regal Earth", sut.SelectedThemeName);
        Assert.Equal("Choose a theme and tap Apply Theme.", sut.ThemeChangeResponse);
        Assert.Equal(new[] { "Regal Earth", "Wine Desert", "Forest Lavender" }, sut.ThemeOptions);
    }

    [Fact]
    public void ApplyThemeCommand_WithNewSelection_UpdatesThemeAndResponse()
    {
        var themeService = new RecordingThemeService();
        var sut = new ThemeSettingsViewModel(themeService)
        {
            SelectedThemeName = "Wine Desert"
        };

        sut.ApplyThemeCommand.Execute(null);

        Assert.Equal("Wine Desert", themeService.LastRequestedThemeName);
        Assert.Equal("Wine Desert", sut.CurrentThemeName);
        Assert.Equal("Wine Desert", sut.SelectedThemeName);
        Assert.Equal("Theme changed to Wine Desert.", sut.ThemeChangeResponse);
    }

    [Fact]
    public void ApplyThemeCommand_WithMissingSelection_PromptsUserToChooseATheme()
    {
        var themeService = new RecordingThemeService();
        var sut = new ThemeSettingsViewModel(themeService)
        {
            SelectedThemeName = string.Empty
        };

        sut.ApplyThemeCommand.Execute(null);

        Assert.Equal("Select a theme before applying.", sut.ThemeChangeResponse);
        Assert.Equal(0, themeService.SetThemeCallCount);
    }
}
