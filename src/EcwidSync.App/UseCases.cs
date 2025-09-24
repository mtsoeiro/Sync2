namespace EcwidSync.App;
public interface ICategorySyncUseCase
{
    Task<int> SyncAsync(CancellationToken ct = default);
}
