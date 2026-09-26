using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;

namespace Domain.TickTest.Business.Services
{
    // Domain service holding the backtest engine. Receives the context's ticks
    // newest first and returns the updated context: the caller must keep the
    // returned value, since the struct is passed by value.
    public class BacktestCore : IBacktestCore
    {
        public BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return context;
        }
    }
}
