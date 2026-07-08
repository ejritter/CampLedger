using System.Collections.Generic;
using System.Linq;
using CampLedger.Services;
using CampLedger.ViewModels;
using CommunityToolkit.Maui.Extensions;
using MauiLeftSideNavBar.Controls;
using MauiLeftSideNavBar.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CampLedger.Pages;

public sealed class CampLedgerNavigationPage : MauiLeftSideNavBarPage
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CampLedgerNavigationViewModel _viewModel;

    public CampLedgerNavigationPage(CampLedgerNavigationViewModel viewModel, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _viewModel = viewModel;
        Title = "Camp Ledger";
        BuildNavigation();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object? sender, EventArgs e)
    {
        ApplyThemeColors();
    }



    private void BuildNavigation()
    {
        Mode = NavBarMode.Flyout;
        NavBarWidth = 220;
        NavBarHeaderHeight = GridLength.Auto;
        NavBarFooterHeight = GridLength.Auto;

        foreach (var item in _viewModel.NavigationItems)
        {
            // Always register the real title. MauiLeftSideNavBarPage captures this string once
            // (as NavBarItem.Label) and re-applies it to the item's Label.Text itself whenever
            // Mode changes (shown in NavBarMode.Open, hidden/empty in NavBarMode.Flyout) - see
            // its private ApplyMode. Passing an empty string here for Flyout mode would bake
            // that empty value in permanently, so titles would never appear even after
            // switching to Open.
            AddNavItem(item.Title, CreateMenuIcon(item.Glyph), item.PageFactory, item.IsDefault);
        }

        var defaultItem = _viewModel.NavigationItems.FirstOrDefault(item => item.IsDefault);
        if (defaultItem is not null)
        {
            CurrentPageTitle = defaultItem.Title;
        }

        // NavBarHeader/NavBarFooter are (re)built inside ApplyThemeColors so that
        // both the initial build and every later theme change get freshly
        // colored header/footer views - see ApplyThemeColors remarks.
        ApplyThemeColors();
    }





    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(Mode))
        {
            NavBarHeader = CreateHeader();
            NavBarFooter = CreateFooter();
        }
    }


    private View? CreateHeader()
    {
        if (Mode == NavBarMode.Flyout)
        {
            return null;
        }

        return new VerticalStackLayout
        {
            Padding = new Thickness(16, 18),
            Spacing = 4,
            Children =
        {
            new Label
            {
                Text = "Camp Ledger",
                FontAttributes = FontAttributes.Bold,
                FontSize = 20,
                TextColor = ResolveColor("PrimaryTextColor") ?? Colors.White
            },
            new Label
            {
                Text = "Camping checklist",
                FontSize = 12,
                TextColor = ResolveColor("SecondaryTextColor") ?? Colors.White.WithAlpha(0.75f)
            }
        }
        };
    }


    private View CreateFooter()
    {
        var footer = new Grid
        {
            Padding = new Thickness(16, 14),
            HeightRequest = 56,
            BackgroundColor = ResolveColor("SurfaceAltColor") ?? Color.FromArgb("#2A2A3E")
        };

        footer.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await ShowThemePopupAsync())
        });

        var layout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(24) },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 10,
            VerticalOptions = LayoutOptions.Center
        };

        layout.Children.Add(new Label
        {
            FontFamily = "Segoe Fluent Icons",
            FontSize = 18,
            Text = "\uE713",
            TextColor = ResolveColor("PrimaryTextColor") ?? Colors.White,
            VerticalOptions = LayoutOptions.Center
        });

        if (Mode == NavBarMode.Open)
        {
            var title = new Label
            {
                Text = "Theme",
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                TextColor = ResolveColor("PrimaryTextColor") ?? Colors.White,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            layout.Children.Add(title);
            Grid.SetColumn(title, 1);
        }
        footer.Children.Add(layout);
        return footer;
    }

    private ImageSource CreateMenuIcon(string glyph)
    {
        return new FontImageSource
        {
            FontFamily = "Segoe Fluent Icons",
            Glyph = glyph,
            Size = 18,
            Color = ResolveColor("PrimaryColor") ?? Color.FromArgb("#C89B3C")
        };
    }

    /// <remarks>
    /// MauiLeftSideNavBarPage only re-colors already-built nav items from three
    /// internal, private call sites (item construction, item activation on tap,
    /// and Application.Current.RequestedThemeChanged for the OS light/dark
    /// switch) - none of which fire when CampLedger's own theme popup swaps
    /// merged ResourceDictionaries. Setting NavBarBackgroundColor/
    /// ToolbarBackgroundColor/ToolbarTextColor alone therefore only affects the
    /// nav bar's own chrome, not the header/footer views (built once, in C#) or
    /// each nav item's Label/Icon (owned by the library, no public refresh
    /// hook). So every call here also rebuilds the header/footer and reflects
    /// into the nav items via NavBarItemVisualRefresher - see that type's
    /// remarks for the full root-cause writeup of the "nav item shows white
    /// text until clicked" symptom.
    /// </remarks>
    private void ApplyThemeColors()
    {
        Color surfaceColor = ResolveColor("SurfaceColor") ?? Color.FromArgb("#1E1E2E");
        Color surfaceAltColor = ResolveColor("SurfaceAltColor") ?? Color.FromArgb("#2A2A3E");
        Color primaryTextColor = ResolveColor("PrimaryTextColor") ?? Colors.White;
        Color primaryColor = ResolveColor("PrimaryColor") ?? Color.FromArgb("#C89B3C");

        NavBarBackgroundColor = surfaceColor;
        ToolbarBackgroundColor = surfaceAltColor;
        ToolbarTextColor = primaryTextColor;
        CurrentPageTitle = string.IsNullOrWhiteSpace(CurrentPageTitle) ? "CampLedger" : CurrentPageTitle;

        NavBarHeader = CreateHeader();
        NavBarFooter = CreateFooter();

        NavBarItemVisualRefresher.Refresh(this, primaryTextColor, primaryColor, surfaceAltColor);
    }

    private static Color? ResolveColor(string key)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var resource) == true && resource is Color color)
        {
            return color;
        }

        return null;
    }

    private async Task ShowThemePopupAsync()
    {
        var popup = _serviceProvider.GetRequiredService<ThemeSelectionPopup>();
        await this.ShowPopupAsync(popup);
        ApplyThemeColors();
    }
}
