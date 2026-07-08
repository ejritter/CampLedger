using CampLedger.Services;

namespace CampLedger.Pages;

/// <summary>
/// Helpers that work around how the third-party <c>MauiLeftSideNavBarPage</c> hosts pages.
/// </summary>
/// <remarks>
/// <c>MauiLeftSideNavBarPage.ActivateNavItem</c> resolves each nav item's page via its
/// <c>PageFactory</c> and then does <c>MainContentView.Content = page.Content;</c> - it pulls
/// the page's root <see cref="View"/> out of the <see cref="ContentPage"/> and reparents it
/// into the nav bar's own container. The <see cref="ContentPage"/> object itself (<c>page</c>)
/// is then discarded: it is never pushed onto a <see cref="INavigation"/> stack or assigned as
/// a <see cref="Microsoft.Maui.Controls.Window.Page"/>, so it never gets attached to a
/// <see cref="Microsoft.Maui.Controls.Window"/>. This causes two distinct, easy-to-miss
/// failure modes, both fixed by the methods below.
/// </remarks>
internal static class PageBindingContextExtensions
{
    /// <summary>
    /// Sets BindingContext on both the page and its root <see cref="ContentPage.Content"/> so
    /// it survives the nav bar's reparenting.
    /// </summary>
    /// <remarks>
    /// When <c>Content</c> is reparented into the nav bar's container, MAUI tries to propagate
    /// BindingContext down from the new parent
    /// (<see cref="BindableObject.SetInheritedBindingContext"/>), which is a no-op only when
    /// the child already carries an explicitly ("manually") assigned BindingContext. Setting
    /// <c>BindingContext</c> solely on the <see cref="ContentPage"/> is not enough, because the
    /// inherited value on the discarded page's <c>Content</c> is exactly what the nav bar's
    /// reparenting overwrites. Explicitly assigning BindingContext to the root <c>Content</c>
    /// view as well keeps it a "manual" value that the reparenting cannot clobber, so
    /// bindings/commands underneath it keep working after the page is hosted.
    /// </remarks>
    public static void AttachViewModel(this ContentPage page, object viewModel)
    {
        page.BindingContext = viewModel;

        if (page.Content is not null)
        {
            page.Content.BindingContext = viewModel;
        }
    }

    /// <summary>
    /// Returns the page to call DisplayAlertAsync/DisplayPromptAsync/ShowPopupAsync (or similar
    /// dialog/popup APIs) on, instead of calling them on <paramref name="page"/> directly.
    /// </summary>
    /// <remarks>
    /// Because <paramref name="page"/> is never attached to a <see cref="Microsoft.Maui.Controls.Window"/>
    /// (see the type-level remarks), <paramref name="page"/>.Window is always null and
    /// <paramref name="page"/>.Navigation is a <c>NavigationProxy</c> whose <c>Inner</c> is
    /// always null too. MAUI's DisplayAlertAsync silently no-ops when Window is null - it logs
    /// "Window is null, alert will not be shown" and resolves the result to false/null instead
    /// of throwing. CommunityToolkit.Maui's ShowPopupAsync degrades even more subtly: with
    /// Navigation.Inner null, NavigationProxy.OnPushModal queues the popup push and returns an
    /// already-completed task instead of throwing, so the popup is never actually shown, and
    /// the awaited PopupClosed TaskCompletionSource then never completes - the call just hangs
    /// forever. This is why dialogs and popups launched from these pages appeared to do
    /// nothing (or, for popups, silently never returned). This returns the app's actual current
    /// window's page instead, which is attached and can show dialogs/popups.
    /// </remarks>
    public static Page GetPresentingPage(this ContentPage page)
    {
        return CurrentPageProvider.Current ?? page;
    }
}
