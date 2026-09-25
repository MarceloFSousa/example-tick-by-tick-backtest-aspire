using System;

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
        public double StopPrice;
        public EOrderStatus Status;
        public DateTime CreatedAt;
        public double FilledQuantity;
        public double AverageFillPrice;
        public DateTime? FilledAt;

        public override string ToString() => $"{Asset} {Side} {Type} {Quantity}@{Price} {Status} Filled:{FilledQuantity}@{AverageFillPrice}";
    }
}
