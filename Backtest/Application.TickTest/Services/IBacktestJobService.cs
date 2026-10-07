using Domain.TickTest.Models;

namespace Application.TickTest.Services
{
    public interface IBacktestJobService
    {
        // Queues a backtest and returns its job (Pending) right away.
        BacktestJob Enqueue(BacktestRequest request);
        Task<BacktestJob?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        Task<BacktestReport?> GetReportAsync(Guid id, CancellationToken cancellationToken = default);
        // Ids of the queued jobs, in the order they were queued.
        IAsyncEnumerable<Guid> ReadQueueAsync(CancellationToken cancellationToken = default);
        // Runs a queued job to its end and saves its report.
        Task ProcessAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
