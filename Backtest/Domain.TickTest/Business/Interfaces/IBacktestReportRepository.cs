using Domain.TickTest.Models;

namespace Domain.TickTest.Business.Interfaces
{
    public interface IBacktestReportRepository
    {
        // Returns the path of the saved report.
        Task<string> SaveAsync(BacktestReport report, CancellationToken cancellationToken = default);
        Task<BacktestReport?> GetAsync(Guid id, CancellationToken cancellationToken = default);
        string GetPath(Guid id);
    }
}
