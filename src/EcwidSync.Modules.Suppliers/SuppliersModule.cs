// EcwidSync.Modules.Suppliers/SuppliersModule.cs
using EcwidSync.Shared.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace EcwidSync.Modules.Suppliers;

public sealed class SuppliersModule : IModule
{
    public string Name => "Fornecedores";
    public string? IconKey => "fornecedores";          // mapeia para um ícone no teu MainWindow
    public Type EntryViewType => typeof(Views.SuppliersView);

    public void RegisterServices(IServiceCollection s)
    {
        s.AddTransient<Views.SuppliersView>();
        s.AddTransient<ViewModels.SuppliersViewModel>();
        // Importers
        s.AddTransient<Abstractions.ISupplierImporter, Importers.AlsoTxtImporter>();
        s.AddTransient<Abstractions.ISupplierImporter, Importers.EetCsvImporter>();

        // Orquestrador
        s.AddScoped<Services.ISupplierImportService, Services.SupplierImportService>();
    }
}
