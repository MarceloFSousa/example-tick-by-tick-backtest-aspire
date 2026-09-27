using Domain.TickTest.Business.Services;
using Domain.TickTest.Models;
using Tests.TickTest.Support;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class TradeRulesTests
    {
        // ---- GetCurrentTick ----

        [Fact]
        public void GetCurrentTick_SeveralTicks_ReturnsNewest()
        {
            var context = Empty().WithTick(100, minute: 0).WithTick(101, minute: 1);

            var tick = TradeRules.GetCurrentTick(context);

            Assert.Equal(101, tick.Price);
            Assert.Equal(T0.AddMinutes(1), tick.Timestamp);
        }

        [Fact]
        public void GetCurrentTick_NullTicks_Throws()
        {
            var context = Empty();
            context.Ticks = null!;

            Assert.Throws<InvalidOperationException>(() => TradeRules.GetCurrentTick(context));
        }

        [Fact]
        public void GetCurrentTick_NoTicks_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => TradeRules.GetCurrentTick(Empty()));
        }

        // ---- IsHit ----

        [Theory]
        // Limit buy waits for the price to fall to it.
        [InlineData(EOrderType.Limit, EOrderSide.Buy, 95, 96, false)]
        [InlineData(EOrderType.Limit, EOrderSide.Buy, 95, 95, true)]
        [InlineData(EOrderType.Limit, EOrderSide.Buy, 95, 94, true)]
        // Limit sell waits for the price to rise to it.
        [InlineData(EOrderType.Limit, EOrderSide.Sell, 105, 104, false)]
        [InlineData(EOrderType.Limit, EOrderSide.Sell, 105, 105, true)]
        [InlineData(EOrderType.Limit, EOrderSide.Sell, 105, 106, true)]
        // Stop buy triggers when the price rises to it.
        [InlineData(EOrderType.Stop, EOrderSide.Buy, 108, 107, false)]
        [InlineData(EOrderType.Stop, EOrderSide.Buy, 108, 108, true)]
        [InlineData(EOrderType.Stop, EOrderSide.Buy, 108, 109, true)]
        // Stop sell triggers when the price falls to it.
        [InlineData(EOrderType.Stop, EOrderSide.Sell, 90, 91, false)]
        [InlineData(EOrderType.Stop, EOrderSide.Sell, 90, 90, true)]
        [InlineData(EOrderType.Stop, EOrderSide.Sell, 90, 89, true)]
        public void IsHit_LimitAndStopOrders_FollowsPriceRules(EOrderType type, EOrderSide side, double orderPrice, double tickPrice, bool expected)
        {
            var order = new Order { Type = type, Side = side, Price = orderPrice };

            Assert.Equal(expected, TradeRules.IsHit(order, tickPrice));
        }

        [Theory]
        [InlineData(EOrderSide.Buy)]
        [InlineData(EOrderSide.Sell)]
        public void IsHit_MarketOrder_IsNeverHit(EOrderSide side)
        {
            var order = new Order { Type = EOrderType.Market, Side = side, Price = 100 };

            Assert.False(TradeRules.IsHit(order, 100));
            Assert.False(TradeRules.IsHit(order, 1));
            Assert.False(TradeRules.IsHit(order, 1000));
        }

        // ---- Fill ----

        [Fact]
        public void Fill_BuyOnFlat_OpensLong()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Buy, 2, 100, T0);

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(2, context.Position.Quantity);
            Assert.Equal(100, context.Position.AveragePrice);
            Assert.Equal(T0, context.Position.OpenAt);
            Assert.Equal(TestAsset.Ticker, context.Position.Asset.Ticker);
            Assert.Empty(context.ClosedPositions);
        }

        [Fact]
        public void Fill_SellOnFlat_OpensShort()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Sell, 3, 100, T0);

            Assert.Equal(EPositionSide.Short, context.Position.Side);
            Assert.Equal(3, context.Position.Quantity);
            Assert.Equal(100, context.Position.AveragePrice);
        }

        [Fact]
        public void Fill_SameSide_AddsAndReaverages()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Buy, 2, 100, T0);

            // Unequal quantities, so a plain average of the two prices would not pass.
            context = TradeRules.Fill(context, EOrderSide.Buy, 1, 110, T0.AddMinutes(1));

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(3, context.Position.Quantity);
            Assert.Equal(310.0 / 3, context.Position.AveragePrice, 9);
            Assert.Equal(T0, context.Position.OpenAt);
            Assert.Empty(context.ClosedPositions);
        }

        [Fact]
        public void Fill_OppositeSide_ReducesAndRecordsClosedPosition()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Buy, 4, 105, T0);

            context = TradeRules.Fill(context, EOrderSide.Sell, 1, 120, T0.AddMinutes(2));

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(3, context.Position.Quantity);
            Assert.Equal(105, context.Position.AveragePrice);

            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(TestAsset.Ticker, closed.Asset.Ticker);
            Assert.Equal(EPositionSide.Long, closed.Side);
            Assert.Equal(1, closed.Quantity);
            Assert.Equal(105, closed.EntryPrice);
            Assert.Equal(120, closed.ExitPrice);
            Assert.Equal(T0, closed.OpenAt);
            Assert.Equal(T0.AddMinutes(2), closed.CloseAt);
            Assert.Equal(15, closed.PnL);
        }

        [Fact]
        public void Fill_ExactClose_GoesFlat()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Buy, 2, 100, T0);

            context = TradeRules.Fill(context, EOrderSide.Sell, 2, 110, T0.AddMinutes(1));

            Assert.Equal(0, context.Position.Quantity);
            Assert.Equal(default, context.Position);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(2, closed.Quantity);
            Assert.Equal(20, closed.PnL);
        }

        [Fact]
        public void Fill_LargerThanPosition_FlipsSide()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Buy, 2, 100, T0);

            context = TradeRules.Fill(context, EOrderSide.Sell, 5, 90, T0.AddMinutes(1));

            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(EPositionSide.Long, closed.Side);
            Assert.Equal(2, closed.Quantity);
            Assert.Equal(-20, closed.PnL);

            Assert.Equal(EPositionSide.Short, context.Position.Side);
            Assert.Equal(3, context.Position.Quantity);
            Assert.Equal(90, context.Position.AveragePrice);
            Assert.Equal(T0.AddMinutes(1), context.Position.OpenAt);
        }

        [Fact]
        public void Fill_CloseShortBelowEntry_Profits()
        {
            var context = TradeRules.Fill(Empty(), EOrderSide.Sell, 2, 100, T0);

            context = TradeRules.Fill(context, EOrderSide.Buy, 2, 90, T0.AddMinutes(1));

            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(EPositionSide.Short, closed.Side);
            Assert.Equal(20, closed.PnL);
            Assert.Equal(0, context.Position.Quantity);
        }
    }
}
