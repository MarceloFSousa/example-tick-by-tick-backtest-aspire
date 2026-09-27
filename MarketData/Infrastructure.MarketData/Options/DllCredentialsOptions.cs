namespace Infrastructure.MarketData.Options
{
    // Mirrors Domain.DLL.Settings.DLLAuthParams plus the Exchange value DLLService
    // takes separately. Kept as a standalone POCO (not referencing Domain.DLL) since
    // Infrastructure.MarketData doesn't depend on Domain.DLL yet - mapping onto the
    // real DLLAuthParams happens once the DLL-backed IMarketDataProvider is built.
    public class DllCredentialsOptions
    {
        public string Key { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RoutingPassword { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
    }
}
