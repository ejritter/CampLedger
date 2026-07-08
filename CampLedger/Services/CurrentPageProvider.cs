namespace CampLedger.Services;

/// <summary>
/// Resolves the app's actual, currently-displayed root <see cref="Page"/>.
/// </summary>
/// <remarks>
/// CampLedger hosts its pages via the third-party <c>MauiLeftSideNavBarPage</c> rather than
/// <see cref="Shell"/> (see <c>CampLedgerNavigationPage</c>), so <see cref="Shell.Current"/> is
/// always <see langword="null"/> here. <see cref="Current"/> instead reads the root page
/// directly off <see cref="Application.Current"/>'s active window, which is reliable regardless
/// of whether Shell is in use. Use this wherever a ViewModel needs a page reference to call
/// DisplayAlertAsync/DisplayPromptAsync or similar page-scoped dialog APIs.
/// </remarks>
public static class CurrentPageProvider
{
    public static Page? Current
    {
        get
        {
            return Application.Current?.Windows.FirstOrDefault()?.Page;
        }
    }
}
