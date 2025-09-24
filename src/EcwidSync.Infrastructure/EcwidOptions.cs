namespace EcwidSync.Infrastructure;
public sealed class EcwidOptions
{
    public string StoreId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://app.ecwid.com/api/v3/";
}
