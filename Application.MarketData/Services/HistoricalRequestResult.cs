namespace Application.MarketData.Services
{
    public record HistoricalRequestResult(IReadOnlyList<DateTime> RequestedDays, IReadOnlyList<DateTime> SkippedDays);
}
