using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;

namespace Domain.TickTest.Business.Services
{
    // Domain service holding the backtest engine. Walks the context's ticks in the
    // order given (newest first).
    public class BacktestCore : IBacktestCore
    {
        public void Run(BacktestContext context, CancellationToken cancellationToken = default)
        {
            foreach (var tick in context.Ticks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // TODO: engine (strategy / order matching / positions) goes here
            }
        }
    }
}
