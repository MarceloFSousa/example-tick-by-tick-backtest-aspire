using System;
using System.Collections.Generic;

namespace Domain.TickTest.Models
{
    public struct BacktestContext
    {
        public Asset Asset;
        // Ordered by Timestamp descending (newest first). The core walks them in this order and does not sort.
        public List<TradeTick> Ticks;
        // Open position for the asset; Quantity 0 means flat (Side is only meaningful when Quantity > 0).
        public Position Position;
        // Positions that went flat, in the order they were closed.
        public List<ClosedPosition> ClosedPositions;
        // Every order the engine created; Status New means still pending.
        public List<Order> Orders;

        public override string ToString() => $"{Asset} Ticks:{Ticks?.Count ?? 0}";
    }
}
