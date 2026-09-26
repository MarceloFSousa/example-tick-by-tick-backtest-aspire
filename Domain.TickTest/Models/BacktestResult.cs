using System;
using System.Collections.Generic;

namespace Domain.TickTest.Models
{
    public struct BacktestResult
    {
        public IReadOnlyList<DateTime> ProcessedDays;
        public IReadOnlyList<DateTime> SkippedDays;
        public long TickCount;
        public double RealizedPnL;
        public Position OpenPosition;
        public IReadOnlyList<Position> ClosedPositions;
        public IReadOnlyList<Order> Orders;

        public override string ToString() => $"Processed:{ProcessedDays?.Count ?? 0} Skipped:{SkippedDays?.Count ?? 0} Ticks:{TickCount} PnL:{RealizedPnL} Closed:{ClosedPositions?.Count ?? 0} Orders:{Orders?.Count ?? 0}";
    }
}
