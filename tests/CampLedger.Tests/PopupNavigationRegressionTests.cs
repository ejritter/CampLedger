using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;

namespace CampLedger.Tests;

/// <summary>
/// Regression tests for the "adding/changing a Trip Location does nothing" bug.
///
/// TripLedgerPage/TripHistoryPage are hosted by the third-party MauiLeftSideNavBarPage, which
/// extracts their root <see cref="ContentPage.Content"/> and discards the page itself (see
/// <see cref="PageBindingContextExtensions"/> remarks) - so these pages are never attached to a
/// <see cref="Window"/> and their <see cref="NavigationElement.Navigation"/> proxy's
/// <c>Inner</c> is always null. CommunityToolkit.Maui's
/// <c>PopupExtensions.ShowPopupAsync(this Page, ...)</c> calls
/// <c>page.Navigation.PushModalAsync(popupPage, false)</c> to display the popup, then awaits a
/// TaskCompletionSource that only completes when the popup later raises PopupClosed.
///
/// These tests prove the real <see cref="NavigationProxy"/> mechanism that breaks this: when
/// <c>Inner</c> is null, <c>PushModalAsync</c> does not throw and does not actually display
/// anything - it just queues the request internally and returns an already-completed task. So
/// <c>ShowPopupAsync</c> sails past that await with no error, then hangs forever awaiting a
/// PopupClosed that will never fire, because the popup was never really shown. That is why
/// clicking the Trip Location button appeared to do nothing instead of erroring.
/// </summary>
public class PopupNavigationRegressionTests
{
    [Fact]
    public void Navigation_InnerIsNull_ForPageNeverAttachedToWindow()
    {
        // This is the precondition the whole bug depends on: TripLedgerPage/TripHistoryPage are
        // constructed, have their Content torn out and reparented into the nav bar, and are
        // then discarded - they are never pushed onto a navigation stack nor assigned as a
        // Window.Page, so nothing ever sets their NavigationProxy.Inner.
        var orphanedPage = new ContentPage { Content = new Grid() };

        var proxy = Assert.IsType<NavigationProxy>(orphanedPage.Navigation);

        Assert.Null(proxy.Inner);
    }

    [Fact]
    public async Task PushModalAsync_CompletesImmediately_OnUnattachedPage_WithoutActuallyDisplayingAnything()
    {
        // Reproduces exactly what CommunityToolkit.Maui's ShowPopupAsync does internally:
        // "await navigation.PushModalAsync(popupPage, false)". On an unattached page's
        // NavigationProxy (Inner == null), NavigationProxy.OnPushModal only adds the request to
        // an internal pending queue and returns Task.FromResult<object>(null) - an
        // already-completed task - instead of throwing or actually presenting the modal. The
        // push silently "succeeds" with nothing ever appearing on screen.
        var orphanedPage = new ContentPage { Content = new Grid() };
        var modalPage = new ContentPage { Content = new Grid() };

        var pushTask = orphanedPage.Navigation.PushModalAsync(modalPage, animated: false);

        Assert.True(pushTask.IsCompletedSuccessfully);

        // The proxy's own bookkeeping believes the push happened (it queued it for whenever
        // Inner might eventually be set), which is exactly what makes this failure mode so hard
        // to spot: nothing throws, and even the ModalStack looks populated as expected, yet the
        // popup was never actually rendered because Inner never gets assigned for these pages.
        await pushTask;
        Assert.Contains(modalPage, orphanedPage.Navigation.ModalStack);
    }
}
