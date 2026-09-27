using System;

namespace Domain.MarketData.Models
{
    public struct TradeTick
    {
        public Guid Id;
        public Asset Asset;
        public DateTime Timestamp;
        public double Price;
        public double Quantity;
        public ETradeType Type;
        public Agent Buyer;
        public Agent Seller;

        public override string ToString() => $"{Asset} {Timestamp:O} {Type} {Quantity}@{Price} Buyer:{Buyer} Seller:{Seller}";
    }
}
