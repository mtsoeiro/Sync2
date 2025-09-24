using System.Collections.Generic;
using System.Threading;
using EcwidSync.Domain;
using EcwidSync.Infrastructure;

namespace EcwidSync.Modules.Categories.ViewModels;

public sealed class CategoriesService : ICategoriesService
{
    private readonly IEcwidClient _client;
    public CategoriesService(IEcwidClient client) => _client = client;

    public IAsyncEnumerable<(Category category, string rawJson)>
        GetAllCategoriesWithRawAsync(int pageSize, CancellationToken ct)
        => _client.GetAllCategoriesWithRawAsync(pageSize, ct);
}
