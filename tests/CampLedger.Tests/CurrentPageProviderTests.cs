using CampLedger.Services;

namespace CampLedger.Tests;

/// <summary>
/// Regression test for the dialog/DisplayAlertAsync no-op bug: pages hosted by
/// MauiLeftSideNavBarPage are never attached to a Window, so calling DisplayAlertAsync
/// directly on them silently resolves to false/null instead of showing anything (see
/// PageBindingContextExtensions.GetPresentingPage). CurrentPageProvider.Current is the
/// replacement for the unreliable Shell.Current in this app.
/// </summary>
public class CurrentPageProviderTests
{
    [Fact]
    public void Current_ReturnsNull_WhenNoApplicationIsRunning()
    {
        // No Microsoft.Maui.Controls.Application is bootstrapped in the xunit process, so
        // Application.Current is null here. CurrentPageProvider must not throw in that case -
        // it should degrade to null so callers can check for it, exactly like they now do in
        // TripHistoryViewModel.DeleteTrip/OpenLocation instead of dereferencing Shell.Current
        // (which was always null in this app, since it hosts pages via MauiLeftSideNavBarPage,
        // not Shell) and throwing a NullReferenceException.
        var presentingPage = CurrentPageProvider.Current;

        Assert.Null(presentingPage);
    }
}
