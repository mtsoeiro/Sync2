using Microsoft.Extensions.DependencyInjection;
using System;
namespace EcwidSync.Modules.Products;

using EcwidSync.Modules.Products.ViewModels;
using EcwidSync.Shared.Modularity;

public sealed class ProductsModule : IModule
{
    public string Name => "Produtos";
    public string? IconKey => "produtos";
    public Type EntryViewType => typeof(Views.ProductsView);
    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<ViewModels.ProductsViewModel>();
        services.AddTransient<Views.ProductsView>();
        services.AddScoped<ViewModels.IProductsService, ProductsService>();
    }
}
