using System;
using Domain.TickTest.Enums;

namespace Domain.TickTest.Models
{
    public struct ClosedPosition
    {
        public Asset Asset;
        public EPositionSide Side;
        public double Quantity;
        public double EntryPrice;
        public double ExitPrice;
        public DateTime OpenAt;
        public DateTime CloseAt;

        // Derived from the fields above, so it never goes out of sync with them.
        public double PnL => (Side == EPositionSide.Long ? ExitPrice - EntryPrice : EntryPrice - ExitPrice) * Quantity;

        public override string ToString() => $"{Asset} {Side} {Quantity} {EntryPrice}->{ExitPrice} PnL:{PnL}";
    }
}
