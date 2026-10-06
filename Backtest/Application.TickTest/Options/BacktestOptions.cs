namespace Application.TickTest.Options
{
    // Defaults of the Backtest section, used when a request does not bring the value.
    public class BacktestOptions
    {
        // Cost charged per contract on every fill (entry and exit).
        public double CostPerContract { get; set; }
    }
}
