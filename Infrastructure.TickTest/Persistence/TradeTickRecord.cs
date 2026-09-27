using Domain.TickTest.Models;

namespace Infrastructure.TickTest.Persistence
{
    // Flat, settable-property shape required by the Parquet/CSV serializers, kept
    // separate from the domain TradeTick struct so storage concerns (column layout,
    // enum-as-string, etc.) don't leak into Domain.TickTest.
    public class TradeTickRecord
    {
        public Guid Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; }
        public double Price { get; set; }
        public double Quantity { get; set; }
        public string Type { get; set; } = string.Empty;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public int SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;

        public static TradeTickRecord FromDomain(TradeTick tick) => new()
        {
            Id = tick.Id,
            Ticker = tick.Asset.Ticker ?? string.Empty,
            Exchange = tick.Asset.Exchange ?? string.Empty,
            TimestampUtc = tick.Timestamp,
            Price = tick.Price,
            Quantity = tick.Quantity,
            Type = tick.Type.ToString(),
            BuyerId = tick.Buyer.Id,
            BuyerName = tick.Buyer.Name ?? string.Empty,
            SellerId = tick.Seller.Id,
            SellerName = tick.Seller.Name ?? string.Empty
        };

        public TradeTick ToDomain() => new()
        {
            Id = Id,
            Asset = new Asset { Ticker = Ticker, Exchange = Exchange },
            Timestamp = TimestampUtc,
            Price = Price,
            Quantity = Quantity,
            Type = Enum.Parse<ETradeType>(Type),
            Buyer = new Agent { Id = BuyerId, Name = BuyerName },
            Seller = new Agent { Id = SellerId, Name = SellerName }
        };
    }
}
