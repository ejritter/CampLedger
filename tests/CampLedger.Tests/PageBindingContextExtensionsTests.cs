using CampLedger.Pages;
using Microsoft.Maui.Controls;

namespace CampLedger.Tests;

/// <summary>
/// Regression tests for the BindingContext-loss bug caused by
/// <c>MauiLeftSideNavBarPage.ActivateNavItem</c> reparenting a hosted page's root
/// <see cref="ContentPage.Content"/> into the nav bar's own container
/// (<c>MainContentView.Content = page.Content;</c>). That reparenting makes MAUI
/// re-propagate BindingContext down from the new parent via
/// <see cref="BindableObject.SetInheritedBindingContext"/>, which only refuses to overwrite
/// a child's BindingContext when that child already carries an explicitly-assigned
/// ("manual") value. These tests simulate that reparenting directly against the real MAUI
/// BindableObject inheritance mechanism, without needing a running app or the nav bar
/// package itself.
/// </summary>
public class PageBindingContextExtensionsTests
{
    private sealed class FakePageViewModel
    {
    }

    private sealed class FakeNavBarContainerViewModel
    {
    }

    [Fact]
    public void ContentBindingContext_IsOverwritten_WhenOnlyPageBindingContextIsSet()
    {
        // This reproduces the original bug: setting BindingContext only on the
        // ContentPage lets the reparented Content's BindingContext get clobbered by
        // the nav bar's own inherited propagation, because the Content's BindingContext
        // was never itself an explicit ("manual") value - only inherited from the page.
        var pageViewModel = new FakePageViewModel();
        var page = new ContentPage
        {
            Content = new Grid(),
        };
        page.BindingContext = pageViewModel;

        Assert.Same(pageViewModel, page.Content.BindingContext);

        var navBarContainerViewModel = new FakeNavBarContainerViewModel();
        BindableObject.SetInheritedBindingContext(page.Content, navBarContainerViewModel);

        Assert.NotSame(pageViewModel, page.Content.BindingContext);
    }

    [Fact]
    public void AttachViewModel_KeepsContentBindingContext_AfterSimulatedNavBarReparenting()
    {
        var pageViewModel = new FakePageViewModel();
        var page = new ContentPage
        {
            Content = new Grid(),
        };

        page.AttachViewModel(pageViewModel);

        Assert.Same(pageViewModel, page.BindingContext);
        Assert.Same(pageViewModel, page.Content.BindingContext);

        // Simulate MauiLeftSideNavBarPage.ActivateNavItem's
        // `MainContentView.Content = page.Content;` reparenting, which triggers MAUI to
        // try to propagate BindingContext down from the nav bar's own container.
        var navBarContainerViewModel = new FakeNavBarContainerViewModel();
        BindableObject.SetInheritedBindingContext(page.Content, navBarContainerViewModel);

        Assert.Same(pageViewModel, page.Content.BindingContext);
    }

    [Fact]
    public void GetPresentingPage_FallsBackToPageItself_WhenNoLiveApplicationWindowExists()
    {
        // In the xunit process there is no Microsoft.Maui.Controls.Application bootstrapped,
        // so CurrentPageProvider.Current (Application.Current?.Windows...) is naturally null -
        // the same "unattached" state a MauiLeftSideNavBarPage-hosted ContentPage is in at
        // runtime. GetPresentingPage must degrade gracefully to the page itself rather than
        // throwing or returning null, so callers can still safely await DisplayAlertAsync
        // (it will just no-op, instead of crashing).
        var page = new ContentPage
        {
            Content = new Grid(),
        };

        var presentingPage = page.GetPresentingPage();

        Assert.Same(page, presentingPage);
    }
}
