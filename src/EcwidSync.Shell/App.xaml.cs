using EcwidSync.Modules.Categories;
using EcwidSync.Modules.Categories.ViewModels;
using EcwidSync.Modules.Suppliers.Services;
using EcwidSync.Persistence;
using EcwidSync.Shared.Modularity;
using EcwidSync.Shared.Notifications;
using EcwidSync.Shell.Dashboard;
using EcwidSync.Shell.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using Wpf.Ui;

namespace EcwidSync.Shell;

public partial class App : Application
{
    public static IHost Host { get; private set; } = default!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Host = CreateHostBuilder().Build();
        await Host.StartAsync(); // assíncrono
        await DbInit.EnsureUpToDateAsync(Host.Services, devAutoRecreateIfMissing: true);

        // cria/atualiza a base de dados
        using (var scope = Host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync(); //garante BD OK
        }

        var main = Host.Services.GetRequiredService<MainWindow>();
        main.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Host.StopAsync().GetAwaiter().GetResult();
        } 
        catch
        {
            /* ignore */
        }
        Host.Dispose();
        base.OnExit(e);
    }

    private static IHostBuilder CreateHostBuilder() =>
        Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureLogging(lb => lb.AddConsole())
            .ConfigureServices((ctx, services) =>
            {
                // UI principal
                services.AddSingleton<MainWindow>();

                // Ecwid
                services.Configure<EcwidSync.Infrastructure.EcwidOptions>(ctx.Configuration.GetSection("Ecwid"));
                services.AddHttpClient<EcwidSync.Infrastructure.IEcwidClient, EcwidSync.Infrastructure.EcwidClient>();

                // DB
                var cs = ctx.Configuration.GetConnectionString("Sql")
                         ?? "Server=.\\SQLEXPRESS;Database=EcwidSync;Trusted_Connection=True;TrustServerCertificate=True";
                services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

                // Persistence
                services.AddScoped<IProductStore, EfProductStore>();
                services.AddScoped<ICategoryStore, EfCategoryStore>();

                // Notificações (WPF UI)
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IAppNotifier, AppNotifier>();
                services.AddScoped<EcwidSync.Persistence.ISupplierStore, EcwidSync.Persistence.EfSupplierStore>();
                // Dashboard
                services.AddSingleton<DashboardPanel>();
                services.AddSingleton<DashboardPanelViewModel>();
                services.AddScoped<IDashboardService, DashboardService>();
                services.AddScoped<ISupplierStore, EfSupplierStore>();
                services.AddScoped<ISupplierImportService, SupplierImportService>();

                // Módulos
                IModule[] modules =
                {
                    new EcwidSync.Modules.Products.ProductsModule(),
                    new EcwidSync.Modules.Categories.CategoriesModule(),
                    new EcwidSync.Modules.Suppliers.SuppliersModule(),

                };
                services.AddSingleton<IModuleCatalog>(_ => new ModuleCatalog(modules));
                foreach (var m in modules) m.RegisterServices(services);
            });
}
