using Domain.TickTest.Models;

namespace Web.TickTest.DTOs
{
    public record BacktestApiRequest(string Ticker, string Exchange, DateTime Start, DateTime End);

    // Report is null until the process ends.
    public record BacktestStatusResponse(BacktestJob Job, BacktestReport? Report);
}
