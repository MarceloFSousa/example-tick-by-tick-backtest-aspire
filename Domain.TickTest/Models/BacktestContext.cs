using System;
using System.Collections.Generic;

namespace Domain.TickTest.Models
{
    public struct BacktestContext
    {
        public Asset Asset;
        // Chronological (Timestamp ascending, oldest first): the handler appends each tick as it is replayed, so
        // the current tick is the LAST one (Ticks[^1], see TradeRules.GetCurrentTick) and the history is before it.
        public List<TradeTick> Ticks;
        // Open position for the asset; Quantity 0 means flat (Side is only meaningful when Quantity > 0).
        public Position Position;
        // Each reduction/close of the position appends one entry for the closed quantity, in order.
        public List<ClosedPosition> ClosedPositions;
        // Every order the engine created; Status New means still pending.
        public List<Order> Orders;

        public override string ToString() => $"{Asset} Ticks:{Ticks?.Count ?? 0}";

    }
}
