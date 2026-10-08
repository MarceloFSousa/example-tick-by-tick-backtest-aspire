using Domain.TickTest.Business.Services;
using Domain.TickTest.Models;
using Domain.TickTest.Enums;
using Tests.TickTest.Support;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class TradeServiceTests
    {
        // ---- SendOrder ----

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void SendOrder_NonPositiveQuantity_Throws(double quantity)
        {
            var context = Empty().WithTick(100);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TradeService.SendOrder(context, MarketOrder(EOrderSide.Buy, quantity)));
        }

        [Theory]
        [InlineData(EOrderType.Limit, 0)]
        [InlineData(EOrderType.Limit, -5)]
        [InlineData(EOrderType.Stop, 0)]
        [InlineData(EOrderType.Stop, -5)]
        public void SendOrder_LimitOrStopWithoutPositivePrice_Throws(EOrderType type, double price)
        {
            var context = Empty().WithTick(100);
            var order = new Order { Side = EOrderSide.Buy, Type = type, Quantity = 1, Price = price };

            Assert.Throws<ArgumentException>(() => TradeService.SendOrder(context, order));
        }

        [Fact]
        public void SendOrder_NoTick_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                TradeService.SendOrder(Empty(), MarketOrder(EOrderSide.Buy, 1)));
        }

        [Fact]
        public void SendOrder_MarketOrder_FillsAtNewestTick()
        {
            var context = Empty().WithTick(100, minute: 3);

            context = TradeService.SendOrder(context, MarketOrder(EOrderSide.Buy, 2));

            var order = Assert.Single(context.Orders);
            Assert.Equal(EOrderStatus.Filled, order.Status);
            Assert.Equal(100, order.Price);
            Assert.Equal(T0.AddMinutes(3), order.CreatedAt);
            Assert.Equal(T0.AddMinutes(3), order.FilledAt);
            Assert.NotEqual(Guid.Empty, order.Id);
            Assert.Equal(TestAsset.Ticker, order.Asset.Ticker);

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(2, context.Position.Quantity);
            Assert.Equal(100, context.Position.AveragePrice);
        }

        [Fact]
        public void SendOrder_OrderWithId_KeepsId()
        {
            var id = Guid.NewGuid();
            var order = MarketOrder(EOrderSide.Buy, 1);
            order.Id = id;

            var context = TradeService.SendOrder(Empty().WithTick(100), order);

            Assert.Equal(id, Assert.Single(context.Orders).Id);
        }

        [Fact]
        public void SendOrder_LimitOrder_StaysNewAndKeepsPosition()
        {
            var context = Empty().WithTick(100, minute: 1);

            context = TradeService.SendOrder(context, LimitOrder(EOrderSide.Buy, 1, 95));

            var order = Assert.Single(context.Orders);
            Assert.Equal(EOrderStatus.New, order.Status);
            Assert.Null(order.FilledAt);
            Assert.Equal(95, order.Price);
            Assert.Equal(T0.AddMinutes(1), order.CreatedAt);
            Assert.NotEqual(Guid.Empty, order.Id);
            Assert.Equal(0, context.Position.Quantity);
        }

        // ---- CancelOrder ----

        [Fact]
        public void CancelOrder_PendingOrder_BecomesCanceled()
        {
            var context = TradeService.SendOrder(Empty().WithTick(100), LimitOrder(EOrderSide.Buy, 1, 95));

            context = TradeService.CancelOrder(context, context.Orders[0].Id);

            Assert.Equal(EOrderStatus.Canceled, Assert.Single(context.Orders).Status);
        }

        [Fact]
        public void CancelOrder_UnknownOrder_Throws()
        {
            var context = Empty().WithTick(100);

            Assert.Throws<InvalidOperationException>(() => TradeService.CancelOrder(context, Guid.NewGuid()));
        }

        [Fact]
        public void CancelOrder_FilledOrder_Throws()
        {
            var context = TradeService.SendOrder(Empty().WithTick(100), MarketOrder(EOrderSide.Buy, 1));
            var id = context.Orders[0].Id;

            Assert.Throws<InvalidOperationException>(() => TradeService.CancelOrder(context, id));
        }

        [Fact]
        public void CancelOrder_CanceledOrder_Throws()
        {
            var context = TradeService.SendOrder(Empty().WithTick(100), LimitOrder(EOrderSide.Buy, 1, 95));
            var id = context.Orders[0].Id;
            context = TradeService.CancelOrder(context, id);

            Assert.Throws<InvalidOperationException>(() => TradeService.CancelOrder(context, id));
        }

        // ---- OpenPosition / ClosePosition ----

        [Fact]
        public void OpenPosition_Long_SendsMarketBuy()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 2);

            var order = Assert.Single(context.Orders);
            Assert.Equal(EOrderSide.Buy, order.Side);
            Assert.Equal(EOrderType.Market, order.Type);
            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(2, context.Position.Quantity);
        }

        [Fact]
        public void OpenPosition_Short_SendsMarketSell()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Short, 3);

            var order = Assert.Single(context.Orders);
            Assert.Equal(EOrderSide.Sell, order.Side);
            Assert.Equal(EPositionSide.Short, context.Position.Side);
            Assert.Equal(3, context.Position.Quantity);
        }

        [Fact]
        public void ClosePosition_Long_SellsFullQuantity()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 2);

            context = TradeService.ClosePosition(context.WithTick(110, minute: 1));

            Assert.Equal(0, context.Position.Quantity);
            Assert.Equal(2, context.Orders.Count);
            Assert.Equal(EOrderSide.Sell, context.Orders[1].Side);
            Assert.Equal(2, context.Orders[1].Quantity);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(EPositionSide.Long, closed.Side);
            Assert.Equal(20, closed.PnL);
        }

        [Fact]
        public void ClosePosition_Short_BuysFullQuantity()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Short, 3);

            context = TradeService.ClosePosition(context.WithTick(90, minute: 1));

            Assert.Equal(0, context.Position.Quantity);
            Assert.Equal(EOrderSide.Buy, context.Orders[1].Side);
            Assert.Equal(3, context.Orders[1].Quantity);
            Assert.Equal(30, Assert.Single(context.ClosedPositions).PnL);
        }

        [Fact]
        public void ClosePosition_Flat_Throws()
        {
            var context = Empty().WithTick(100);

            Assert.Throws<InvalidOperationException>(() => TradeService.ClosePosition(context));
        }

        // ---- VerifyOpenOrders ----

        [Theory]
        // order type, side, order price, new tick price, filled?, expected fill price
        [InlineData(EOrderType.Limit, EOrderSide.Buy, 95, 94, true, 95)]
        [InlineData(EOrderType.Limit, EOrderSide.Buy, 95, 95, true, 95)]
        [InlineData(EOrderType.Limit, EOrderSide.Buy, 95, 96, false, 0)]
        [InlineData(EOrderType.Limit, EOrderSide.Sell, 105, 106, true, 105)]
        [InlineData(EOrderType.Limit, EOrderSide.Sell, 105, 104, false, 0)]
        [InlineData(EOrderType.Stop, EOrderSide.Buy, 108, 109, true, 109)]
        [InlineData(EOrderType.Stop, EOrderSide.Buy, 108, 107, false, 0)]
        [InlineData(EOrderType.Stop, EOrderSide.Sell, 90, 89, true, 89)]
        [InlineData(EOrderType.Stop, EOrderSide.Sell, 90, 91, false, 0)]
        public void VerifyOpenOrders_PendingOrder_FillsOnlyWhenHit(
            EOrderType type, EOrderSide side, double orderPrice, double tickPrice, bool filled, double fillPrice)
        {
            var order = new Order { Type = type, Side = side, Quantity = 1, Price = orderPrice };
            var context = TradeService.SendOrder(Empty().WithTick(100, minute: 0), order);

            context = TradeService.VerifyOpenOrders(context.WithTick(tickPrice, minute: 1));

            var result = Assert.Single(context.Orders);
            if (!filled)
            {
                Assert.Equal(EOrderStatus.New, result.Status);
                Assert.Null(result.FilledAt);
                Assert.Equal(0, context.Position.Quantity);
                return;
            }

            Assert.Equal(EOrderStatus.Filled, result.Status);
            Assert.Equal(T0.AddMinutes(1), result.FilledAt);
            Assert.Equal(side == EOrderSide.Buy ? EPositionSide.Long : EPositionSide.Short, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(fillPrice, context.Position.AveragePrice);
        }

        [Fact]
        public void VerifyOpenOrders_CalledTwiceOnSameTick_DoesNotRefill()
        {
            var context = TradeService.SendOrder(Empty().WithTick(100), LimitOrder(EOrderSide.Buy, 1, 95));
            context = context.WithTick(94, minute: 1);

            context = TradeService.VerifyOpenOrders(context);
            context = TradeService.VerifyOpenOrders(context);

            Assert.Equal(1, context.Position.Quantity);
            Assert.Empty(context.ClosedPositions);
        }

        [Fact]
        public void VerifyOpenOrders_CanceledOrder_IsNeverFilled()
        {
            var context = TradeService.SendOrder(Empty().WithTick(100), LimitOrder(EOrderSide.Buy, 1, 95));
            context = TradeService.CancelOrder(context, context.Orders[0].Id);

            context = TradeService.VerifyOpenOrders(context.WithTick(90, minute: 1));

            Assert.Equal(EOrderStatus.Canceled, Assert.Single(context.Orders).Status);
            Assert.Equal(0, context.Position.Quantity);
        }

        [Fact]
        public void VerifyOpenOrders_SeveralHits_FillsInCreationOrderAndNets()
        {
            var context = Empty().WithTick(100);
            context = TradeService.SendOrder(context, LimitOrder(EOrderSide.Buy, 2, 95));  // created first
            context = TradeService.SendOrder(context, StopOrder(EOrderSide.Sell, 1, 96));  // created second

            // 94 hits both: the limit buy fills at 95 (long 2), then the stop sell at the tick price 94.
            context = TradeService.VerifyOpenOrders(context.WithTick(94, minute: 1));

            Assert.All(context.Orders, o => Assert.Equal(EOrderStatus.Filled, o.Status));
            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(95, context.Position.AveragePrice);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(1, closed.Quantity);
            Assert.Equal(95, closed.EntryPrice);
            Assert.Equal(94, closed.ExitPrice);
        }

        [Fact]
        public void VerifyOpenOrders_NoTick_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => TradeService.VerifyOpenOrders(Empty()));
        }

        // ---- Take profit / stop loss ----

        [Fact]
        public void OpenPosition_WithTakeProfitAndStopLoss_SetsThemOnThePosition()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 1,
                takeProfitPrice: 110, stopLossPrice: 95);

            Assert.Equal(110, context.Position.TakeProfit!.Value.Price);
            Assert.Equal(95, context.Position.StopLoss!.Value.Price);
            Assert.Single(context.Orders);
        }

        [Theory]
        [InlineData(0.0, null)]
        [InlineData(-1.0, null)]
        [InlineData(null, 0.0)]
        [InlineData(null, -1.0)]
        public void SendOrder_NonPositiveTakeProfitOrStopLoss_Throws(double? takeProfitPrice, double? stopLossPrice)
        {
            var context = Empty().WithTick(100);
            var order = MarketOrder(EOrderSide.Buy, 1);
            order.TakeProfitPrice = takeProfitPrice;
            order.StopLossPrice = stopLossPrice;

            Assert.Throws<ArgumentException>(() => TradeService.SendOrder(context, order));
        }

        [Fact]
        public void VerifyOpenOrders_TakeProfitHit_ClosesAtItsPriceAndCancelsStopLoss()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 1,
                takeProfitPrice: 110, stopLossPrice: 95);

            context = TradeService.VerifyOpenOrders(context.WithTick(111, minute: 1));

            Assert.Equal(0, context.Position.Quantity);
            Assert.Null(context.Position.TakeProfit);
            Assert.Null(context.Position.StopLoss);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(110, closed.ExitPrice);
            Assert.Equal(10, closed.PnL);

            Assert.Equal(3, context.Orders.Count);
            var takeProfit = Assert.Single(context.Orders, o => o.Type == EOrderType.Limit);
            Assert.Equal(EOrderStatus.Filled, takeProfit.Status);
            Assert.Equal(T0.AddMinutes(1), takeProfit.FilledAt);
            var stopLoss = Assert.Single(context.Orders, o => o.Type == EOrderType.Stop);
            Assert.Equal(EOrderStatus.Canceled, stopLoss.Status);
        }

        [Fact]
        public void VerifyOpenOrders_StopLossHit_ClosesAtTickPriceAndCancelsTakeProfit()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 1,
                takeProfitPrice: 110, stopLossPrice: 95);

            context = TradeService.VerifyOpenOrders(context.WithTick(94, minute: 1));

            Assert.Equal(0, context.Position.Quantity);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(94, closed.ExitPrice);
            Assert.Equal(-6, closed.PnL);

            var stopLoss = Assert.Single(context.Orders, o => o.Type == EOrderType.Stop);
            Assert.Equal(EOrderStatus.Filled, stopLoss.Status);
            Assert.Equal(94, stopLoss.Price);
            var takeProfit = Assert.Single(context.Orders, o => o.Type == EOrderType.Limit);
            Assert.Equal(EOrderStatus.Canceled, takeProfit.Status);
        }

        [Theory]
        [InlineData(89, 90, 10)]   // take profit: limit buy fills at its own price
        [InlineData(106, 106, -6)] // stop loss: stop buy fills at the tick price
        public void VerifyOpenOrders_ShortProtectionHit_ClosesPosition(double tickPrice, double exitPrice, double pnl)
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Short, 1,
                takeProfitPrice: 90, stopLossPrice: 105);

            context = TradeService.VerifyOpenOrders(context.WithTick(tickPrice, minute: 1));

            Assert.Equal(0, context.Position.Quantity);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(exitPrice, closed.ExitPrice);
            Assert.Equal(pnl, closed.PnL);
        }

        [Fact]
        public void VerifyOpenOrders_ProtectionNotHit_KeepsPositionAndOrders()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 1,
                takeProfitPrice: 110, stopLossPrice: 95);

            context = TradeService.VerifyOpenOrders(context.WithTick(105, minute: 1));

            Assert.Equal(1, context.Position.Quantity);
            Assert.NotNull(context.Position.TakeProfit);
            Assert.NotNull(context.Position.StopLoss);
            Assert.Single(context.Orders);
            Assert.Empty(context.ClosedPositions);
        }

        [Fact]
        public void VerifyOpenOrders_OnlyStopLossHit_FillsWithoutCanceledOrder()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 1, stopLossPrice: 95);

            context = TradeService.VerifyOpenOrders(context.WithTick(95, minute: 1));

            Assert.Equal(0, context.Position.Quantity);
            Assert.Equal(2, context.Orders.Count);
            Assert.All(context.Orders, o => Assert.Equal(EOrderStatus.Filled, o.Status));
        }

        [Fact]
        public void VerifyOpenOrders_PendingLimitWithTakeProfitAndStopLoss_SetsThemWhenItFills()
        {
            var order = LimitOrder(EOrderSide.Buy, 1, 98);
            order.TakeProfitPrice = 110;
            order.StopLossPrice = 95;
            var context = TradeService.SendOrder(Empty().WithTick(100), order);
            Assert.Null(context.Position.TakeProfit);

            context = TradeService.VerifyOpenOrders(context.WithTick(97, minute: 1));

            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(110, context.Position.TakeProfit!.Value.Price);
            Assert.Equal(95, context.Position.StopLoss!.Value.Price);
        }

        [Fact]
        public void ClosePosition_WithProtectionOrders_DropsThemWithoutRecording()
        {
            var context = TradeService.OpenPosition(Empty().WithTick(100), EPositionSide.Long, 1,
                takeProfitPrice: 110, stopLossPrice: 95);

            context = TradeService.ClosePosition(context.WithTick(102, minute: 1));

            Assert.Null(context.Position.TakeProfit);
            Assert.Null(context.Position.StopLoss);
            Assert.Equal(2, context.Orders.Count);
        }

        // ---- End to end ----

        [Fact]
        public void TradeService_FullSequence_SumsToExpectedPnL()
        {
            var context = Empty().WithTick(100, minute: 0);
            context = TradeService.OpenPosition(context, EPositionSide.Long, 2);                 // long 2 @100
            context = TradeService.OpenPosition(context.WithTick(110, minute: 1), EPositionSide.Long, 1); // long 3 @103.33
            context = TradeService.SendOrder(context.WithTick(120, minute: 2), MarketOrder(EOrderSide.Sell, 1)); // reduce 1
            context = TradeService.SendOrder(context.WithTick(90, minute: 3), MarketOrder(EOrderSide.Sell, 5));  // close 2, short 3 @90
            context = TradeService.ClosePosition(context.WithTick(80, minute: 4));               // close short 3

            var avg = 310.0 / 3;
            var expected = (120 - avg) + 2 * (90 - avg) + 3 * (90 - 80);
            Assert.Equal(expected, context.ClosedPositions.Sum(p => p.PnL), 9);
            Assert.Equal(3, context.ClosedPositions.Count);
            Assert.Equal(0, context.Position.Quantity);
            Assert.Equal(5, context.Orders.Count);
            Assert.All(context.Orders, o => Assert.Equal(EOrderStatus.Filled, o.Status));
        }
    }
}
