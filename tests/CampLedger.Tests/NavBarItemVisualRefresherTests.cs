using System.Collections.Generic;
using CampLedger.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CampLedger.Tests;

/// <summary>
/// Regression tests for the "MauiLeftSideNavBar doesn't fully respond to theme changes"
/// bug: MauiLeftSideNavBarPage only re-colors a nav item's Label/Icon/Container from three
/// private call sites (item construction, item activation on tap, and
/// Application.Current.RequestedThemeChanged for the OS light/dark switch). None of those
/// fire when CampLedger's own theme popup swaps merged ResourceDictionaries, so already-built
/// nav items kept stale colors (e.g. white label text) until clicked. NavBarItemVisualRefresher
/// reflects into the library's private "_navItems"/"_activeNavItem" fields to force a refresh.
///
/// These tests exercise the exact field/property names confirmed via decompiling the real
/// MauiLeftSideNavBar.Controls.MauiLeftSideNavBarPage (private List&lt;NavBarItem&gt; _navItems,
/// private NavBarItem? _activeNavItem, and NavBarItem.LabelView/IconView/Container), using a
/// local fake shaped the same way - the real package only ships platform-specific TFMs
/// (net10.0-android/-ios/-maccatalyst/-windows), so this plain net10.0 test project cannot
/// reference it directly.
/// </summary>
public class NavBarItemVisualRefresherTests
{
    private sealed class FakeNavItem
    {
        public Label? LabelView { get; set; }

        public Image? IconView { get; set; }

        public Border? Container { get; set; }
    }

    /// <summary>
    /// Mirrors MauiLeftSideNavBarPage: the nav items list and active-item pointer are
    /// private instance fields declared on this (base) type.
    /// </summary>
    private class FakeNavBarHost
    {
        private readonly List<FakeNavItem> _navItems = new();
        private FakeNavItem? _activeNavItem;

        public FakeNavItem AddItem()
        {
            var item = new FakeNavItem
            {
                LabelView = new Label(),
                IconView = new Image { Source = new FontImageSource { Glyph = "\uE80F" } },
                Container = new Border(),
            };
            _navItems.Add(item);
            return item;
        }

        public void Activate(FakeNavItem item)
        {
            _activeNavItem = item;
        }
    }

    /// <summary>
    /// Mirrors CampLedgerNavigationPage : MauiLeftSideNavBarPage - an empty subclass with no
    /// members of its own, so navBarPage.GetType() (called from within Refresh) returns this
    /// derived type while "_navItems"/"_activeNavItem" remain declared on the base type.
    /// </summary>
    private sealed class FakeNavBarHostSubclass : FakeNavBarHost
    {
    }

    [Fact]
    public void Refresh_UpdatesLabelTextColor_ForItemsDeclaredOnBaseType()
    {
        // This is the scenario that matters in production: CampLedgerNavigationPage.GetType()
        // returns the derived type, but _navItems lives on the MauiLeftSideNavBarPage base
        // class. BindingFlags.NonPublic alone does not search inherited private instance
        // fields, so without walking the type hierarchy this would silently find nothing.
        var host = new FakeNavBarHostSubclass();
        var item1 = host.AddItem();
        item1.LabelView!.TextColor = Colors.White;
        var item2 = host.AddItem();
        item2.LabelView!.TextColor = Colors.White;

        var expectedTextColor = Color.FromArgb("#1E1E1E");
        NavBarItemVisualRefresher.Refresh(host, expectedTextColor, Colors.Black, Colors.Gray);

        Assert.Equal(expectedTextColor, item1.LabelView.TextColor);
        Assert.Equal(expectedTextColor, item2.LabelView.TextColor);
    }

    [Fact]
    public void Refresh_UpdatesFontImageSourceIconColor()
    {
        var host = new FakeNavBarHostSubclass();
        var item = host.AddItem();
        var fontImageSource = Assert.IsType<FontImageSource>(item.IconView!.Source);
        fontImageSource.Color = Colors.White;

        var expectedIconColor = Color.FromArgb("#C89B3C");
        NavBarItemVisualRefresher.Refresh(host, Colors.Black, expectedIconColor, Colors.Gray);

        Assert.Equal(expectedIconColor, fontImageSource.Color);
    }

    [Fact]
    public void Refresh_HighlightsActiveItemContainer_AndClearsOthers()
    {
        var host = new FakeNavBarHostSubclass();
        var activeItem = host.AddItem();
        var inactiveItem = host.AddItem();
        host.Activate(activeItem);

        var expectedActiveBackground = Color.FromArgb("#2A2A3E");
        NavBarItemVisualRefresher.Refresh(host, Colors.Black, Colors.Black, expectedActiveBackground);

        Assert.Equal(expectedActiveBackground, activeItem.Container!.BackgroundColor);
        Assert.Equal(Colors.Transparent, inactiveItem.Container!.BackgroundColor);
    }

    [Fact]
    public void Refresh_DoesNotThrow_WhenNavBarPageIsNull()
    {
        var exception = Record.Exception(() => NavBarItemVisualRefresher.Refresh(null, Colors.Black, Colors.Black, Colors.Black));

        Assert.Null(exception);
    }

    [Fact]
    public void Refresh_DoesNotThrow_WhenExpectedFieldsAreMissing()
    {
        // An object with none of the expected private fields (e.g. a future package rewrite
        // that renames "_navItems") must degrade to a silent no-op, not throw and break theme
        // switching for the rest of the app.
        var unrelatedObject = new object();

        var exception = Record.Exception(() => NavBarItemVisualRefresher.Refresh(unrelatedObject, Colors.Black, Colors.Black, Colors.Black));

        Assert.Null(exception);
    }
}
