using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Domain.TickTest.Enums;

namespace Domain.TickTest.Business.Services
{
    // Domain service holding the backtest engine. Runs once per tick: the context's
    // Ticks are chronological and the last one is the current tick. Returns the
    // updated context: the caller must keep the returned value, and so must every
    // TradeService call here, since the struct is passed by value (only its lists
    // are shared, the open Position is not).
    //
    // First simple strategy: a tick larger than 500 in quantity is a signal in the
    // direction of its aggressor (Buyer -> buy, Seller -> sell). Flat, it opens a
    // position of 1; holding the opposite side, it reverses (close + open).
    public class BacktestCore : IBacktestCore
    {
        private const double SignalQuantity = 500;

        public BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            context = TradeService.VerifyOpenOrders(context);

            var tick = TradeRules.GetCurrentTick(context);
            var buySignal = tick.Quantity > SignalQuantity && tick.Type == ETradeType.Buyer;
            var sellSignal = tick.Quantity > SignalQuantity && tick.Type == ETradeType.Seller;

            if (context.Position.Quantity == 0)
            {
                if (buySignal)
                    context = TradeService.OpenPosition(context, EPositionSide.Long, 1);
                else if (sellSignal)
                    context = TradeService.OpenPosition(context, EPositionSide.Short, 1);
            }
            else if (context.Position.Side == EPositionSide.Long)
            {
                if (sellSignal)
                {
                    context = TradeService.ClosePosition(context);
                    context = TradeService.OpenPosition(context, EPositionSide.Short, 1);
                }
            }
            else if (context.Position.Side == EPositionSide.Short)
            {
                if (buySignal)
                {
                    context = TradeService.ClosePosition(context);
                    context = TradeService.OpenPosition(context, EPositionSide.Long, 1);
                }
            }

            return context;
        }
    }
}
