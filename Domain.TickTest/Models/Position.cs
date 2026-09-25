using System;

namespace Domain.TickTest.Models
{
    public struct Position
    {
        public Asset Asset;
        // Signed: > 0 long, < 0 short, 0 flat.
        public double Quantity;
        public double AveragePrice;
        public double RealizedPnL;
        public DateTime OpenedAt;
        public DateTime? ClosedAt;

        public override string ToString() => $"{Asset} {Quantity}@{AveragePrice} PnL:{RealizedPnL}";
    }
}
