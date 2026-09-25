namespace Application.TickTest.Handlers
{
    public record BacktestResult(IReadOnlyList<DateTime> ProcessedDays, IReadOnlyList<DateTime> SkippedDays, long TickCount);
}
