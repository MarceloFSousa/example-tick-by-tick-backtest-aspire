namespace Infrastructure.MarketData.Options
{
    public class StorageOptions
    {
        public string Provider { get; set; } = "Parquet";
        public string RootPath { get; set; } = string.Empty;
    }
}
