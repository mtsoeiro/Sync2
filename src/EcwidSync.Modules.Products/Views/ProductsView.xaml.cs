using EcwidSync.Modules.Products.ViewModels;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
namespace EcwidSync.Modules.Products.Views;
public partial class ProductsView : UserControl
{
    public ProductsView(ProductsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, __) =>
        {
            if (DataContext is ProductsViewModel vm)
                vm.LoadLocalCommand.Execute(null);
        };
    }
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Uri?.AbsoluteUri))
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
