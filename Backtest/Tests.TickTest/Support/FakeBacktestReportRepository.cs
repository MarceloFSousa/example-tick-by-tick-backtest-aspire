using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;

namespace Tests.TickTest.Support
{
    // In-memory report repository.
    public class FakeBacktestReportRepository : IBacktestReportRepository
    {
        public Dictionary<Guid, BacktestReport> Reports { get; } = new();
        public Exception? SaveFailure;

        public Task<string> SaveAsync(BacktestReport report, CancellationToken cancellationToken = default)
        {
            if (SaveFailure is not null)
                throw SaveFailure;
            Reports[report.Id] = report;
            return Task.FromResult(GetPath(report.Id));
        }

        public Task<BacktestReport?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Reports.TryGetValue(id, out var report) ? report : (BacktestReport?)null);

        public string GetPath(Guid id) => $"{id}.json";
    }
}
