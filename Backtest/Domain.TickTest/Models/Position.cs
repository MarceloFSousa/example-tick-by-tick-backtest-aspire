using System;
using Domain.TickTest.Enums;

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
        // Protection orders (null = none), on the side opposite to the position and with its quantity:
        // TakeProfit is a Limit, StopLoss a Stop. OCO: when one fills, the other is canceled.
        public Order? TakeProfit;
        public Order? StopLoss;

        public override string ToString() => $"{Asset} {Side} {Quantity}@{AveragePrice}";
    }
}
