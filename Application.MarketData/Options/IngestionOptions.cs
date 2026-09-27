namespace Application.MarketData.Options
{
    public class IngestionOptions
    {
        // When true, ticks that are not an aggression (Buyer/Seller) are dropped before storage.
        public bool IgnoreNonAggression { get; set; } = true;
    }
}
