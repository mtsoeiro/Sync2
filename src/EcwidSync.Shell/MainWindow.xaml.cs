using EcwidSync.Shared.Modularity;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace EcwidSync.Shell;

public interface IModuleCatalog
{
    System.Collections.Generic.IReadOnlyList<IModule> Modules { get; }
}

public sealed class ModuleCatalog : IModuleCatalog
{
    public System.Collections.Generic.IReadOnlyList<IModule> Modules { get; }
    public ModuleCatalog(System.Collections.Generic.IEnumerable<IModule> modules) =>
        Modules = new System.Collections.Generic.List<IModule>(modules);
}

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _sp;
    private readonly IModuleCatalog _catalog;

    public MainWindow(IServiceProvider sp, IModuleCatalog catalog, ISnackbarService snackbarService)
    {
        InitializeComponent();
        _sp = sp;
        _catalog = catalog;
        snackbarService.SetSnackbarPresenter(AppSnackbar);

        

        // v4: passa o ServiceProvider para o NavigationView
        RootNav.SetServiceProvider(_sp);
        RootNav.MenuItemsSource = new ObservableCollection<NavigationViewItem>(
           _catalog.Modules.Select(m => new NavigationViewItem
           {
               Content = m.Name,
               TargetPageType = m.EntryViewType,
               Icon = BuildIcon(m.IconKey)
           }));
        Loaded += (_, __) =>
        {
            if (_catalog.Modules.Count > 0)
                RootNav.Navigate(_catalog.Modules[0].EntryViewType); // em vez de Navigate(0)
        };

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.F8) ToggleDashboard();
        };
    }


    private void ToggleDashboard_Click(object sender, RoutedEventArgs e) => ToggleDashboard();

    private void ToggleDashboard()
    {
        DashboardOverlay.Visibility =
            DashboardOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    // --- ícones por chave ----------------------------------------------------

    private static readonly System.Collections.Generic.Dictionary<string, SymbolRegular> IconMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["dashboard"] = SymbolRegular.ChartMultiple24,
        ["produtos"] = SymbolRegular.Box24,
        ["categorias"] = SymbolRegular.Folder24,
        ["fornecedores"] = SymbolRegular.BuildingRetailMoney24,
    };

    private static IconElement BuildIcon(string? key)
    {
        var sym = IconMap.TryGetValue(key ?? "", out var s) ? s : SymbolRegular.Apps24;
        return new SymbolIcon { Symbol = sym };
    }
}
