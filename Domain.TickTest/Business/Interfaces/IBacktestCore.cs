using Domain.TickTest.Models;

namespace Domain.TickTest.Business.Interfaces
{
    public interface IBacktestCore
    {
        void Run(BacktestContext context, CancellationToken cancellationToken = default);
    }
}
