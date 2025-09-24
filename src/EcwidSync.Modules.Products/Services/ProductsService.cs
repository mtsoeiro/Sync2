using EcwidSync.Domain;
using EcwidSync.Infrastructure;

namespace EcwidSync.Modules.Products.ViewModels;

public sealed class ProductsService : IProductsService
{
    private readonly IEcwidClient _client;
    public ProductsService(IEcwidClient client) => _client = client;

    public IAsyncEnumerable<(Product product, string rawJson)> GetAllProductsWithRawAsync(int pageSize, CancellationToken ct)
        => _client.GetAllProductsWithRawAsync(pageSize, ct);
}
