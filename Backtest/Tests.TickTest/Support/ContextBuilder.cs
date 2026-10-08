using Domain.TickTest.Models;
using Domain.TickTest.Enums;

namespace Tests.TickTest.Support
{
    // Test helpers to build BacktestContext/Order values with few lines.
    public static class ContextBuilder
    {
        public static readonly Asset TestAsset = new() { Ticker = "WINFUT", Exchange = "F" };
        public static readonly DateTime T0 = new(2025, 1, 2, 10, 0, 0);

        public static BacktestContext Empty() => new()
        {
            Asset = TestAsset,
            Ticks = new List<TradeTick>(),
            ClosedPositions = new List<ClosedPosition>(),
            Orders = new List<Order>()
        };

        // Ticks are chronological (oldest first), so each new tick goes to the end of the list and the
        // last one is the current tick.
        public static BacktestContext WithTick(this BacktestContext context, double price, int minute = 0,
            double quantity = 1, ETradeType type = ETradeType.Buyer)
        {
            context.Ticks.Add(new TradeTick
            {
                Asset = TestAsset,
                Timestamp = T0.AddMinutes(minute),
                Price = price,
                Quantity = quantity,
                Type = type
            });
            return context;
        }

        public static Order MarketOrder(EOrderSide side, double quantity) =>
            new() { Side = side, Type = EOrderType.Market, Quantity = quantity };

        public static Order LimitOrder(EOrderSide side, double quantity, double price) =>
            new() { Side = side, Type = EOrderType.Limit, Quantity = quantity, Price = price };

        public static Order StopOrder(EOrderSide side, double quantity, double price) =>
            new() { Side = side, Type = EOrderType.Stop, Quantity = quantity, Price = price };
    }
}
