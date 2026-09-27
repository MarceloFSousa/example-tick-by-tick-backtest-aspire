using System;

namespace Domain.TickTest.Models
{
    public struct Position
    {
        public Asset Asset;
        public EPositionSide Side;
        // Always >= 0 (the direction is in Side); 0 means flat.
        public double Quantity;
        public double AveragePrice;
        public DateTime OpenAt;

        public override string ToString() => $"{Asset} {Side} {Quantity}@{AveragePrice}";
    }
}
