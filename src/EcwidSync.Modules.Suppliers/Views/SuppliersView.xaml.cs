using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using EcwidSync.Domain.Suppliers;
using EcwidSync.Modules.Suppliers.ViewModels;

namespace EcwidSync.Modules.Suppliers.Views;

public partial class SuppliersView : UserControl
{
    public SuppliersView(SuppliersViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not SuppliersViewModel vm) return;
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var paths = (string[])e.Data.GetData(DataFormats.FileDrop);
        var path = paths.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;

        // deteção simples: extensão ou nome contém "also"/"eet"
        var file = Path.GetFileName(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();

        SupplierKind? kind = ext switch
        {
            ".csv" => SupplierKind.Eet,
            ".txt" => SupplierKind.Also,
            _ => null
        };

        if (kind is null)
        {
            var lower = file.ToLowerInvariant();
            if (lower.Contains("also")) kind = SupplierKind.Also;
            else if (lower.Contains("eet")) kind = SupplierKind.Eet;
        }

        if (kind is null)
        {
            MessageBox.Show(
                "Não foi possível detetar o fornecedor pelo ficheiro. Use os botões 'Importar ALSO…' ou 'Importar EET…'.",
                "Fornecedores", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await vm.ImportFileAsync(kind.Value, path, CancellationToken.None);
    }
}
