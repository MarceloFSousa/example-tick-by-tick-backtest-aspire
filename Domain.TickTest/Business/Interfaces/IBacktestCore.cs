using Domain.TickTest.Models;

namespace Domain.TickTest.Business.Interfaces
{
    public interface IBacktestCore
    {
        BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default);
    }
}
