using Microsoft.Extensions.DependencyInjection;

namespace EcwidSync.Shared.Modularity
{
    public interface IModule
    {
        string Name { get; }
        string? IconKey { get; }
        Type EntryViewType { get; }
        void RegisterServices(IServiceCollection services);
    }
}
