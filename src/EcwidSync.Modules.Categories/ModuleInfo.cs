using System;
using Microsoft.Extensions.DependencyInjection;
using EcwidSync.Shared.Modularity;
using EcwidSync.Modules.Categories.ViewModels;
using EcwidSync.Modules.Categories.Views;

namespace EcwidSync.Modules.Categories;

public sealed class CategoriesModule : IModule
{
    public string Name => "Categorias";
    public string? IconKey => "categorias";
    public Type EntryViewType => typeof(CategoriesView);

    public void RegisterServices(IServiceCollection services)
    {
        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CategoriesView>();
        services.AddScoped<ICategoriesService, CategoriesService>();
    }
}
