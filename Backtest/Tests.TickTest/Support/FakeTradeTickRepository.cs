using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;

namespace Tests.TickTest.Support
{
    // In-memory repository: only what BacktestHandler uses (ExistsAsync/ReadAsync). Ticks are kept in the
    // order they were added, like an appended day file, so tests can store them out of order on purpose.
    public class FakeTradeTickRepository : ITradeTickRepository
    {
        private readonly Dictionary<DateTime, List<TradeTick>> _days = new();

        public FakeTradeTickRepository WithDay(DateTime day, params TradeTick[] ticks)
        {
            _days[day.Date] = ticks.ToList();
            return this;
        }

        public Task<bool> ExistsAsync(string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(_days.TryGetValue(dateUtc.Date, out var ticks) && ticks.Count > 0);

        public Task<IReadOnlyList<TradeTick>> ReadAsync(string ticker, string exchange, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
        {
            var result = new List<TradeTick>();
            foreach (var (day, ticks) in _days)
            {
                if (day >= startUtc.Date && day <= endUtc.Date)
                    result.AddRange(ticks.Where(t => t.Timestamp >= startUtc && t.Timestamp <= endUtc));
            }
            return Task.FromResult<IReadOnlyList<TradeTick>>(result);
        }

        public Task<int> InsertAsync(TradeTick tick, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task InsertRangeAsync(IEnumerable<TradeTick> ticks, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(TradeTick tick, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int id, string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
