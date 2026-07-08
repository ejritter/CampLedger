using System.Collections;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CampLedger.Services;

/// <summary>
/// Works around a MauiLeftSideNavBarPage limitation: it builds each nav item's
/// Label/Icon/Container views once (see its private BuildNavItemButton) and
/// only re-colors them from three internal call sites - initial item
/// construction, item activation on tap (ActivateNavItem), and
/// Application.Current.RequestedThemeChanged (the OS light/dark switch).
/// Confirmed unchanged as of the latest cached package version (1.0.4) - there
/// is no public API to force a repaint of already-built nav items.
///
/// CampLedger's own named-theme switcher (<see cref="ThemeService"/>) swaps
/// merged ResourceDictionaries directly and never touches RequestedTheme, so
/// none of those refresh hooks fire when the user picks a new theme from the
/// footer's theme popup - already-built nav items keep whatever color was
/// resolved when they were first created (often just a hardcoded OS-theme
/// fallback, since the library looks up "NavBar*"-prefixed resource keys that
/// CampLedger's theme dictionaries don't define) until the item is clicked,
/// which is why nav item labels appeared white/invisible until tapped once.
///
/// The backing "_navItems"/"_activeNavItem" fields and the NavBarItem element
/// type are all private, so this helper reflects into them to keep nav items
/// in sync whenever CampLedgerNavigationPage.ApplyThemeColors runs. Reflection
/// is entirely best-effort: if a future package update renames or removes
/// these private members, refresh silently becomes a no-op instead of
/// throwing, leaving the nav bar in today's already-tolerated unrefreshed
/// state rather than crashing theme switching.
/// </summary>
public static class NavBarItemVisualRefresher
{
    private const string NavItemsFieldName = "_navItems";
    private const string ActiveNavItemFieldName = "_activeNavItem";
    private const string LabelViewPropertyName = "LabelView";
    private const string IconViewPropertyName = "IconView";
    private const string ContainerPropertyName = "Container";

    /// <summary>
    /// Re-applies the current theme's text/icon/active-highlight colors to every
    /// nav item already built on <paramref name="navBarPage"/>.
    /// </summary>
    /// <param name="navBarPage">
    /// The nav bar host instance (in practice a MauiLeftSideNavBarPage subclass).
    /// Typed as <see cref="object"/> deliberately: this method only ever touches
    /// it through reflection, and keeping it type-agnostic lets tests exercise
    /// the exact field/property-walking logic without needing a package
    /// reference to MauiLeftSideNavBar (that package only ships platform-specific
    /// TFMs, which the plain net10.0 test project cannot reference).
    /// </param>
    public static void Refresh(object? navBarPage, Color textColor, Color iconColor, Color activeBackgroundColor)
    {
        if (navBarPage is null)
        {
            return;
        }

        try
        {
            // _navItems/_activeNavItem are declared on MauiLeftSideNavBarPage
            // itself, but navBarPage.GetType() returns the most-derived type
            // (CampLedgerNavigationPage). BindingFlags.NonPublic only searches
            // members declared directly on the type passed to GetField - it does
            // NOT search inherited private instance fields on a base class - so
            // the type hierarchy has to be walked manually or these fields are
            // never found and this becomes a silent no-op (the very bug this
            // class exists to fix).
            FieldInfo? navItemsField = FindField(navBarPage.GetType(), NavItemsFieldName);
            FieldInfo? activeNavItemField = FindField(navBarPage.GetType(), ActiveNavItemFieldName);

            if (navItemsField?.GetValue(navBarPage) is not IEnumerable navItems)
            {
                return;
            }

            object? activeNavItem = activeNavItemField?.GetValue(navBarPage);

            foreach (object navItem in navItems)
            {
                RefreshNavItem(navItem, textColor, iconColor, activeBackgroundColor, isActive: ReferenceEquals(navItem, activeNavItem));
            }
        }
        catch
        {
            // Best-effort only - never let a reflection failure break theme switching.
        }
    }

    private static FieldInfo? FindField(Type type, string fieldName)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            FieldInfo? field = current.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is not null)
            {
                return field;
            }
        }

        return null;
    }

    private static void RefreshNavItem(object navItem, Color textColor, Color iconColor, Color activeBackgroundColor, bool isActive)
    {
        Type itemType = navItem.GetType();

        if (itemType.GetProperty(LabelViewPropertyName)?.GetValue(navItem) is Label label)
        {
            label.TextColor = textColor;
        }

        if (itemType.GetProperty(IconViewPropertyName)?.GetValue(navItem) is Image { Source: FontImageSource fontImageSource })
        {
            fontImageSource.Color = iconColor;
        }

        if (itemType.GetProperty(ContainerPropertyName)?.GetValue(navItem) is Border container)
        {
            container.BackgroundColor = isActive ? activeBackgroundColor : Colors.Transparent;
        }
    }
}
