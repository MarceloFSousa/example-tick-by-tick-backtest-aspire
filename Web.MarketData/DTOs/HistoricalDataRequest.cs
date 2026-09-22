namespace Web.MarketData.DTOs
{
    public record HistoricalDataRequest(string Ticker, string Exchange, DateTime Start, DateTime End);

}
