using System.Windows.Controls;
using EcwidSync.Modules.Categories.ViewModels;

namespace EcwidSync.Modules.Categories.Views;

public partial class CategoriesView : UserControl
{
    public CategoriesView(CategoriesViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, __) => vm.LoadLocalCommand.Execute(null);
    }
}
