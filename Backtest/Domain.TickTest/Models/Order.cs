using System;
using Domain.TickTest.Enums;

namespace Domain.TickTest.Models
{
    public struct Order
    {
        public Guid Id;
        public Asset Asset;
        public EOrderSide Side;
        public EOrderType Type;
        public double Quantity;
        public double Price;
        public EOrderStatus Status;
        public DateTime CreatedAt;
        public DateTime? FilledAt;
        // Optional protection prices: when this order fills, they become the position's TakeProfit/StopLoss.
        public double? TakeProfitPrice;
        public double? StopLossPrice;

        public override string ToString() => $"{Asset} {Side} {Type} {Quantity}@{Price} {Status}";
    }
}
