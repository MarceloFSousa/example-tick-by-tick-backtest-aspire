using Domain.TickTest.Models;
using Domain.TickTest.Enums;

namespace Domain.TickTest.Business.Services
{
    // Static domain service that trades against a BacktestContext. The current
    // price/time is the newest tick (Ticks[^1], the last one). Market orders fill immediately and
    // net against the single open position (add / reduce / close / flip); Limit and
    // Stop orders are stored as New until VerifyOpenOrders sees the newest tick hit
    // them. Every method returns the updated context: the caller must keep the
    // returned value, since the struct is passed by value (only its lists are
    // shared). The building blocks live in TradeRules. An order can carry take
    // profit / stop loss prices: when it fills they become the position's
    // protection orders, which VerifyOpenOrders checks too.
    public static class TradeService
    {
        public static BacktestContext SendOrder(BacktestContext context, Order order)
        {
            if (order.Quantity <= 0)
                throw new ArgumentOutOfRangeException(nameof(order), "A quantidade da ordem deve ser maior que zero.");
            if (order.Type != EOrderType.Market && order.Price <= 0)
                throw new ArgumentException("Ordens Limit/Stop precisam de preço maior que zero.", nameof(order));
            if (order.TakeProfitPrice <= 0 || order.StopLossPrice <= 0)
                throw new ArgumentException("Take profit e stop loss precisam de preço maior que zero.", nameof(order));

            var tick = TradeRules.GetCurrentTick(context);

            order.Id = order.Id == Guid.Empty ? Guid.NewGuid() : order.Id;
            order.Asset = context.Asset;
            order.Status = EOrderStatus.New;
            order.CreatedAt = tick.Timestamp;

            if (order.Type == EOrderType.Market)
            {
                order.Price = tick.Price;
                order.Status = EOrderStatus.Filled;
                order.FilledAt = tick.Timestamp;
                context = TradeRules.Fill(context, order.Side, order.Quantity, tick.Price, tick.Timestamp,
                    order.TakeProfitPrice, order.StopLossPrice);
            }

            context.Orders.Add(order);
            return context;
        }

        public static BacktestContext CancelOrder(BacktestContext context, Guid orderId)
        {
            var index = context.Orders.FindIndex(o => o.Id == orderId);
            if (index < 0)
                throw new InvalidOperationException("Ordem não encontrada.");

            var order = context.Orders[index];
            if (order.Status != EOrderStatus.New)
                throw new InvalidOperationException($"Só é possível cancelar ordens pendentes (status atual: {order.Status}).");

            order.Status = EOrderStatus.Canceled;
            context.Orders[index] = order;
            return context;
        }

        public static BacktestContext OpenPosition(BacktestContext context, EPositionSide side, double quantity,
            double? takeProfitPrice = null, double? stopLossPrice = null)
        {
            var order = new Order
            {
                Side = side == EPositionSide.Long ? EOrderSide.Buy : EOrderSide.Sell,
                Type = EOrderType.Market,
                Quantity = quantity,
                TakeProfitPrice = takeProfitPrice,
                StopLossPrice = stopLossPrice
            };
            return SendOrder(context, order);
        }

        public static BacktestContext ClosePosition(BacktestContext context)
        {
            if (context.Position.Quantity <= 0)
                throw new InvalidOperationException("Não há posição aberta para fechar.");

            var order = new Order
            {
                Side = context.Position.Side == EPositionSide.Long ? EOrderSide.Sell : EOrderSide.Buy,
                Type = EOrderType.Market,
                Quantity = context.Position.Quantity
            };
            return SendOrder(context, order);
        }

        // Checks the pending (New) orders against the newest tick and fills the ones it
        // hit, in creation order (each fill nets against the position before the next
        // order is checked). Limit fills at its own price; Stop becomes a market order
        // and fills at the tick price. Then checks the position's take profit / stop loss
        // the same way: the one that was hit fills and closes the position, the other is
        // canceled (OCO), and both go to Orders. Meant to be called once per tick.
        public static BacktestContext VerifyOpenOrders(BacktestContext context)
        {
            var tick = TradeRules.GetCurrentTick(context);

            for (var i = 0; i < context.Orders.Count; i++)
            {
                var order = context.Orders[i];
                if (order.Status != EOrderStatus.New || !TradeRules.IsHit(order, tick.Price))
                    continue;

                var fillPrice = order.Type == EOrderType.Limit ? order.Price : tick.Price;

                order.Status = EOrderStatus.Filled;
                order.FilledAt = tick.Timestamp;
                context.Orders[i] = order;
                context = TradeRules.Fill(context, order.Side, order.Quantity, fillPrice, tick.Timestamp,
                    order.TakeProfitPrice, order.StopLossPrice);
            }

            var positionOrders = new[] { context.Position.StopLoss, context.Position.TakeProfit };
            for (var i = 0; i < positionOrders.Length; i++)
            {
                if (context.Position.Quantity <= 0)
                    break;
                if (positionOrders[i] is not Order order || !TradeRules.IsHit(order, tick.Price))
                    continue;

                // A Stop fills at the tick price, so the order keeps the price it was filled at.
                order.Price = order.Type == EOrderType.Limit ? order.Price : tick.Price;
                order.Status = EOrderStatus.Filled;
                order.FilledAt = tick.Timestamp;
                context.Orders.Add(order);

                if (positionOrders[1 - i] is Order other)
                {
                    other.Status = EOrderStatus.Canceled;
                    context.Orders.Add(other);
                }

                context = TradeRules.Fill(context, order.Side, order.Quantity, order.Price, tick.Timestamp);
                break;
            }

            return context;
        }
    }
}
