using Domain.TickTest.Business.Services;
using Domain.TickTest.Models;
using Tests.TickTest.Support;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class BacktestCoreTests
    {
        private readonly BacktestCore _core = new();

        [Theory]
        [InlineData(100, ETradeType.Buyer)]
        [InlineData(500, ETradeType.Buyer)]    // exactly 500 is not a signal (must be > 500)
        [InlineData(500, ETradeType.Seller)]
        [InlineData(1000, ETradeType.RLP)]     // big, but not a Buyer/Seller aggressor
        [InlineData(1000, ETradeType.Auction)]
        public void Run_NoSignal_DoesNothing(double quantity, ETradeType type)
        {
            var context = Empty().WithTick(100, minute: 0, quantity: quantity, type: type);

            context = _core.Run(context);

            Assert.Equal(0, context.Position.Quantity);
            Assert.Empty(context.Orders);
            Assert.Empty(context.ClosedPositions);
        }

        [Fact]
        public void Run_BuySignalWhenFlat_OpensLongOfOne()
        {
            var context = Empty().WithTick(100, minute: 0, quantity: 600, type: ETradeType.Buyer);

            context = _core.Run(context);

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(100, context.Position.AveragePrice);
            var order = Assert.Single(context.Orders);
            Assert.Equal(EOrderSide.Buy, order.Side);
            Assert.Equal(EOrderStatus.Filled, order.Status);
        }

        [Fact]
        public void Run_SellSignalWhenFlat_OpensShortOfOne()
        {
            var context = Empty().WithTick(100, minute: 0, quantity: 600, type: ETradeType.Seller);

            context = _core.Run(context);

            Assert.Equal(EPositionSide.Short, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(EOrderSide.Sell, Assert.Single(context.Orders).Side);
        }

        [Fact]
        public void Run_UsesTheNewestTickNotTheFirstOne()
        {
            // The first tick is a big buy, the current (last) one is small: no signal must fire.
            var context = Empty()
                .WithTick(100, minute: 0, quantity: 900, type: ETradeType.Buyer)
                .WithTick(101, minute: 1, quantity: 10, type: ETradeType.Buyer);

            context = _core.Run(context);

            Assert.Equal(0, context.Position.Quantity);
            Assert.Empty(context.Orders);
        }

        [Fact]
        public void Run_BigTickAfterSmallOnes_TradesAtTheNewestTickPrice()
        {
            var context = Empty()
                .WithTick(100, minute: 0, quantity: 10)
                .WithTick(105, minute: 1, quantity: 900, type: ETradeType.Buyer);

            context = _core.Run(context);

            Assert.Equal(105, context.Position.AveragePrice);
            Assert.Equal(T0.AddMinutes(1), context.Position.OpenAt);
        }

        [Fact]
        public void Run_SellSignalWhileLong_ReversesToShort()
        {
            var context = _core.Run(Empty().WithTick(100, minute: 0, quantity: 600, type: ETradeType.Buyer));

            context = _core.Run(context.WithTick(110, minute: 1, quantity: 600, type: ETradeType.Seller));

            Assert.Equal(EPositionSide.Short, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(110, context.Position.AveragePrice);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(EPositionSide.Long, closed.Side);
            Assert.Equal(10, closed.PnL);
            Assert.Equal(3, context.Orders.Count);   // open long, close, open short
        }

        [Fact]
        public void Run_BuySignalWhileShort_ReversesToLong()
        {
            var context = _core.Run(Empty().WithTick(100, minute: 0, quantity: 600, type: ETradeType.Seller));

            context = _core.Run(context.WithTick(90, minute: 1, quantity: 600, type: ETradeType.Buyer));

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(90, context.Position.AveragePrice);
            var closed = Assert.Single(context.ClosedPositions);
            Assert.Equal(EPositionSide.Short, closed.Side);
            Assert.Equal(10, closed.PnL);
            Assert.Equal(3, context.Orders.Count);
        }

        [Theory]
        [InlineData(ETradeType.Buyer, EPositionSide.Long)]
        [InlineData(ETradeType.Seller, EPositionSide.Short)]
        public void Run_SignalInTheSameDirection_KeepsThePosition(ETradeType type, EPositionSide side)
        {
            var context = _core.Run(Empty().WithTick(100, minute: 0, quantity: 600, type: type));

            context = _core.Run(context.WithTick(120, minute: 1, quantity: 600, type: type));

            Assert.Equal(side, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Equal(100, context.Position.AveragePrice);
            Assert.Single(context.Orders);
            Assert.Empty(context.ClosedPositions);
        }

        [Fact]
        public void Run_PositionSurvivesBetweenCalls()
        {
            var context = _core.Run(Empty().WithTick(100, minute: 0, quantity: 600, type: ETradeType.Buyer));

            // A quiet tick must not lose (or reopen) the position opened by the previous call.
            context = _core.Run(context.WithTick(101, minute: 1, quantity: 10));

            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(1, context.Position.Quantity);
            Assert.Single(context.Orders);
        }

        [Fact]
        public void Run_PendingOrderHitByTheTick_IsFilledAndKept()
        {
            var context = Empty().WithTick(100, minute: 0, quantity: 10);
            context = TradeService.SendOrder(context, LimitOrder(EOrderSide.Buy, 1, 95));

            context = _core.Run(context.WithTick(94, minute: 1, quantity: 10));

            Assert.Equal(EOrderStatus.Filled, Assert.Single(context.Orders).Status);
            Assert.Equal(EPositionSide.Long, context.Position.Side);
            Assert.Equal(95, context.Position.AveragePrice);
        }

        [Fact]
        public void Run_CanceledToken_Throws()
        {
            var context = Empty().WithTick(100, quantity: 600, type: ETradeType.Buyer);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => _core.Run(context, cts.Token));
        }
    }
}
