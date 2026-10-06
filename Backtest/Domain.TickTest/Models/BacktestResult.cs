using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.TickTest.Models
{
    public struct BacktestResult
    {
        public IReadOnlyList<DateTime> ProcessedDays;
        public IReadOnlyList<DateTime> SkippedDays;
        public long TickCount;
        public Position OpenPosition;
        public IReadOnlyList<ClosedPosition> ClosedPositions;
        public IReadOnlyList<Order> Orders;
        // Total cost of the run: filled contracts x BacktestRequest.CostPerContract.
        public double Costs;

        // The statistics below are derived from the closed positions, so they never
        // go out of sync with them. The open position is not included (realized only).
        // A trade is a ClosedPosition; it wins or loses by its own PnL, before costs
        // (costs are a total of the run, not split per trade).

        // Sum of the trades' PnL, before costs.
        public readonly double GrossPnL => ClosedPositions?.Sum(p => p.PnL) ?? 0;

        // Net of costs.
        public readonly double RealizedPnL => GrossPnL - Costs;

        public readonly int NumberOfTrades => ClosedPositions?.Count ?? 0;

        // Percentage of winning trades, from 0 to 100.
        public readonly double WinRate => NumberOfTrades == 0 ? 0 : 100.0 * ClosedPositions.Count(p => p.PnL > 0) / NumberOfTrades;

        // Sum of the winning trades / sum of the losing trades (absolute). 0 when no trade lost.
        public readonly double ProfitFactor
        {
            get
            {
                if (ClosedPositions is null)
                    return 0;

                var losses = -ClosedPositions.Where(p => p.PnL < 0).Sum(p => p.PnL);
                return losses == 0 ? 0 : ClosedPositions.Where(p => p.PnL > 0).Sum(p => p.PnL) / losses;
            }
        }

        // Average net result per trade.
        public readonly double PayOff => NumberOfTrades == 0 ? 0 : RealizedPnL / NumberOfTrades;

        public override string ToString() => $"Processed:{ProcessedDays?.Count ?? 0} Skipped:{SkippedDays?.Count ?? 0} Ticks:{TickCount} PnL:{RealizedPnL} Closed:{ClosedPositions?.Count ?? 0} Orders:{Orders?.Count ?? 0}";
    }
}
