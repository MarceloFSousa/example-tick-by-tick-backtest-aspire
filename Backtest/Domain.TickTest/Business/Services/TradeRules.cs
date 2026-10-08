using Domain.TickTest.Models;
using Domain.TickTest.Enums;

namespace Domain.TickTest.Business.Services
{
    // Building blocks of TradeService, public so they can be tested directly.
    public static class TradeRules
    {
        public static TradeTick GetCurrentTick(BacktestContext context)
        {
            if (context.Ticks == null || context.Ticks.Count == 0)
                throw new InvalidOperationException("Não há tick atual para definir preço e horário.");

            // Ticks are chronological (oldest first), so the current tick is the last one.
            return context.Ticks[^1];
        }

        public static bool IsHit(Order order, double price) => (order.Type, order.Side) switch
        {
            // Limit buy waits for the price to fall to it, limit sell for it to rise to it.
            (EOrderType.Limit, EOrderSide.Buy) => price <= order.Price,
            (EOrderType.Limit, EOrderSide.Sell) => price >= order.Price,
            // Stop buy triggers when the price rises to it, stop sell when it falls to it.
            (EOrderType.Stop, EOrderSide.Buy) => price >= order.Price,
            (EOrderType.Stop, EOrderSide.Sell) => price <= order.Price,
            _ => false
        };

        // Builds a protection order for the position: opposite side, same quantity, pending.
        // Limit = take profit, Stop = stop loss.
        public static Order BuildProtectionOrder(Position position, EOrderType type, double price, DateTime time) => new()
        {
            Id = Guid.NewGuid(),
            Asset = position.Asset,
            Side = position.Side == EPositionSide.Long ? EOrderSide.Sell : EOrderSide.Buy,
            Type = type,
            Quantity = position.Quantity,
            Price = price,
            Status = EOrderStatus.New,
            CreatedAt = time
        };

        // Nets a fill against the single open position (add / reduce / close / flip).
        // takeProfitPrice/stopLossPrice only apply when the fill opens or adds to a position:
        // a given price replaces that protection order, null keeps the existing one. The
        // protection orders always follow the position quantity and go away with it.
        public static BacktestContext Fill(BacktestContext context, EOrderSide side, double quantity, double price, DateTime time,
            double? takeProfitPrice = null, double? stopLossPrice = null)
        {
            var fillSide = side == EOrderSide.Buy ? EPositionSide.Long : EPositionSide.Short;
            var position = context.Position;

            if (position.Quantity <= 0 || position.Side == fillSide)
            {
                // Flat or same side: open / add and re-average.
                var total = position.Quantity + quantity;
                position.AveragePrice = position.Quantity <= 0
                    ? price
                    : (position.Quantity * position.AveragePrice + quantity * price) / total;
                if (position.Quantity <= 0)
                    position.OpenAt = time;
                position.Asset = context.Asset;
                position.Side = fillSide;
                position.Quantity = total;
                context.Position = Protect(position, takeProfitPrice, stopLossPrice, time);
                return context;
            }

            // Opposite side: close as much as the position holds, one ClosedPosition per closing fill.
            var closedQuantity = Math.Min(position.Quantity, quantity);
            context.ClosedPositions.Add(new ClosedPosition
            {
                Asset = context.Asset,
                Side = position.Side,
                Quantity = closedQuantity,
                EntryPrice = position.AveragePrice,
                ExitPrice = price,
                OpenAt = position.OpenAt,
                CloseAt = time
            });

            var remainderOfPosition = position.Quantity - closedQuantity;
            if (remainderOfPosition > 0)
            {
                position.Quantity = remainderOfPosition;
                context.Position = Protect(position, null, null, time);
                return context;
            }

            context.Position = default;

            var remainderOfOrder = quantity - closedQuantity;
            if (remainderOfOrder > 0)
            {
                // Flip: the rest of the order opens a position on the other side.
                context.Position = Protect(new Position
                {
                    Asset = context.Asset,
                    Side = fillSide,
                    Quantity = remainderOfOrder,
                    AveragePrice = price,
                    OpenAt = time
                }, takeProfitPrice, stopLossPrice, time);
            }

            return context;
        }

        // Replaces the protection orders that got a new price and resizes the kept ones.
        private static Position Protect(Position position, double? takeProfitPrice, double? stopLossPrice, DateTime time)
        {
            position.TakeProfit = Protect(position, position.TakeProfit, EOrderType.Limit, takeProfitPrice, time);
            position.StopLoss = Protect(position, position.StopLoss, EOrderType.Stop, stopLossPrice, time);
            return position;
        }

        private static Order? Protect(Position position, Order? current, EOrderType type, double? price, DateTime time)
        {
            if (price.HasValue)
                return BuildProtectionOrder(position, type, price.Value, time);
            if (current is not Order order)
                return null;

            order.Quantity = position.Quantity;
            return order;
        }
    }
}
