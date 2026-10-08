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

        // Nets a fill against the single open position (add / reduce / close / flip).
        public static BacktestContext Fill(BacktestContext context, EOrderSide side, double quantity, double price, DateTime time)
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
                context.Position = position;
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
                context.Position = position;
                return context;
            }

            context.Position = default;

            var remainderOfOrder = quantity - closedQuantity;
            if (remainderOfOrder > 0)
            {
                // Flip: the rest of the order opens a position on the other side.
                context.Position = new Position
                {
                    Asset = context.Asset,
                    Side = fillSide,
                    Quantity = remainderOfOrder,
                    AveragePrice = price,
                    OpenAt = time
                };
            }

            return context;
        }
    }
}
