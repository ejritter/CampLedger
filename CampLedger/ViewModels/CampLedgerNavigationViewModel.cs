using System.Collections.ObjectModel;
using System.Linq;
using CampLedger.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace CampLedger.ViewModels;

public sealed partial class CampLedgerNavigationViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    public CampLedgerNavigationViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        BuildNavigationItems();
    }

    [ObservableProperty]
    private NavigationPageItemViewModel? selectedItem;

    public ObservableCollection<NavigationPageItemViewModel> NavigationItems { get; } = [];

    private void BuildNavigationItems()
    {
        NavigationItems.Add(new NavigationPageItemViewModel(
            "Inventory",
            "\uE7B8",
            () => _serviceProvider.GetRequiredService<InventoryPage>(),
            isDefault: true));

        NavigationItems.Add(new NavigationPageItemViewModel(
            "Ledger",
            "\uEA37",
            () => _serviceProvider.GetRequiredService<TripLedgerPage>()));

        NavigationItems.Add(new NavigationPageItemViewModel(
            "Travel History",
            "\uE81C",
            () => _serviceProvider.GetRequiredService<TripHistoryPage>()));

        SelectedItem = NavigationItems.FirstOrDefault(item => item.IsDefault);
    }
}

public sealed partial class NavigationPageItemViewModel : ObservableObject
{
    public NavigationPageItemViewModel(string title, string glyph, Func<ContentPage> pageFactory, bool isDefault = false)
    {
        Title = title;
        Glyph = glyph;
        PageFactory = pageFactory;
        IsDefault = isDefault;
    }

    public string Title { get; }

    private string _titleValue { get; set; }

    public string Glyph { get; }

    public Func<ContentPage> PageFactory { get; }

    public bool IsDefault { get; }

    [ObservableProperty]
    private bool isSelected;
}
