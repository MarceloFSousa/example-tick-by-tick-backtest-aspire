namespace Application.MarketData.Services
{
    public record DateRange(DateTime Start, DateTime End);

    public record HistoricalRequestResult(IReadOnlyList<DateRange> RequestedRanges, IReadOnlyList<DateTime> SkippedDays);
}
