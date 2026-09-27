using Domain.TickTest.Models;

namespace Domain.TickTest.Business.Interfaces
{
    public interface ITradeTickRepository
    {
        Task<Guid> InsertAsync(TradeTick tick, CancellationToken cancellationToken = default);
        Task InsertRangeAsync(IEnumerable<TradeTick> ticks, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TradeTick>> ReadAsync(string ticker, string exchange, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(TradeTick tick, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default);
    }
}
