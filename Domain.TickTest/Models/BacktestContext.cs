using System;
using System.Collections.Generic;

namespace Domain.TickTest.Models
{
    public struct BacktestContext
    {
        public Asset Asset;
        // Ordered by Timestamp descending (newest first). The core walks them in this order and does not sort.
        public List<TradeTick> Ticks;

        public override string ToString() => $"{Asset} Ticks:{Ticks?.Count ?? 0}";
    }
}
