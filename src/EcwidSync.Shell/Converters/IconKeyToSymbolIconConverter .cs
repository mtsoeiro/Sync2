using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui;
using Wpf.Ui.Controls;

public sealed class IconKeyToSymbolIconConverter : IValueConverter
{
    private static readonly Dictionary<string, SymbolRegular> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = SymbolRegular.ChartMultiple24,
            ["produtos"] = SymbolRegular.Box24,
            ["categorias"] = SymbolRegular.Folder24
        };

    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        var key = value as string ?? "";
        var sym = Map.TryGetValue(key, out var s) ? s : SymbolRegular.Apps24;
        return new SymbolIcon { Symbol = sym };
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
}
