using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Infrastructure.MarketData.Options;
using Microsoft.Extensions.Options;
using Parquet.Serialization;

namespace Infrastructure.MarketData.Persistence
{
    // Parquet has no row-level update/delete: every mutation reads the whole
    // asset-day file into memory, rewrites it, and atomically swaps it in. That's
    // O(n) per Insert/Update/Delete for that asset-day - acceptable for tick
    // archival/backtest workloads, not for high-frequency single-row mutations.
    // A single global lock serializes all file mutations across every asset/day;
    // simple and safe, coarser than strictly necessary.
    public class ParquetTradeTickRepository : ITradeTickRepository
    {
        private readonly string _rootPath;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public ParquetTradeTickRepository(IOptions<StorageOptions> options)
        {
            _rootPath = options.Value.RootPath;
        }

        public async Task<Guid> InsertAsync(TradeTick tick, CancellationToken cancellationToken = default)
        {
            if (tick.Id == Guid.Empty)
                tick.Id = Guid.NewGuid();

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                var path = GetFilePath(tick.Asset.Exchange, tick.Asset.Ticker, tick.Timestamp);
                var records = await ReadFileAsync(path, cancellationToken);
                records.Add(TradeTickRecord.FromDomain(tick));
                await WriteFileAsync(path, records, cancellationToken);
            }
            finally
            {
                _fileLock.Release();
            }

            return tick.Id;
        }

        public async Task InsertRangeAsync(IEnumerable<TradeTick> ticks, CancellationToken cancellationToken = default)
        {
            var ticksList = ticks as IList<TradeTick> ?? ticks.ToList();
            for (int i = 0; i < ticksList.Count; i++)
            {
                if (ticksList[i].Id == Guid.Empty)
                {
                    var tick = ticksList[i];
                    tick.Id = Guid.NewGuid();
                    ticksList[i] = tick;
                }
            }

            // Grouped by asset-day so each file is read and rewritten once for the
            // whole batch, instead of once per tick.
            var groups = ticksList.GroupBy(t => (t.Asset.Exchange, t.Asset.Ticker, Date: t.Timestamp.Date));

            foreach (var group in groups)
            {
                await _fileLock.WaitAsync(cancellationToken);
                try
                {
                    var path = GetFilePath(group.Key.Exchange, group.Key.Ticker, group.Key.Date);
                    var records = await ReadFileAsync(path, cancellationToken);
                    records.AddRange(group.Select(TradeTickRecord.FromDomain));
                    await WriteFileAsync(path, records, cancellationToken);
                }
                finally
                {
                    _fileLock.Release();
                }
            }
        }

        public async Task<IReadOnlyList<TradeTick>> ReadAsync(string ticker, string exchange, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
        {
            var result = new List<TradeTick>();
            foreach (var date in EnumerateDates(startUtc, endUtc))
            {
                var path = GetFilePath(exchange, ticker, date);
                var records = await ReadFileAsync(path, cancellationToken);
                result.AddRange(records
                    .Where(r => r.TimestampUtc >= startUtc && r.TimestampUtc <= endUtc)
                    .Select(r => r.ToDomain()));
            }
            return result;
        }

        public async Task<bool> UpdateAsync(TradeTick tick, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                var path = GetFilePath(tick.Asset.Exchange, tick.Asset.Ticker, tick.Timestamp);
                var records = await ReadFileAsync(path, cancellationToken);
                var index = records.FindIndex(r => r.Id == tick.Id);
                if (index < 0)
                    return false;

                records[index] = TradeTickRecord.FromDomain(tick);
                await WriteFileAsync(path, records, cancellationToken);
                return true;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<bool> DeleteAsync(Guid id, string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                var path = GetFilePath(exchange, ticker, dateUtc);
                var records = await ReadFileAsync(path, cancellationToken);
                var removed = records.RemoveAll(r => r.Id == id) > 0;
                if (removed)
                    await WriteFileAsync(path, records, cancellationToken);
                return removed;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private string GetFilePath(string exchange, string ticker, DateTime date) =>
            Path.Combine(_rootPath, exchange, ticker, $"{date:yyyy-MM-dd}.parquet");

        private static async Task<List<TradeTickRecord>> ReadFileAsync(string path, CancellationToken cancellationToken)
        {
            if (!File.Exists(path))
                return new List<TradeTickRecord>();

            await using var stream = File.OpenRead(path);
            var result = await ParquetSerializer.DeserializeAsync<TradeTickRecord>(stream, cancellationToken: cancellationToken);
            return result.Data.ToList();
        }

        private static async Task WriteFileAsync(string path, List<TradeTickRecord> records, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = path + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await ParquetSerializer.SerializeAsync(records, stream, cancellationToken: cancellationToken);
            }

            File.Move(tempPath, path, overwrite: true);
        }

        private static IEnumerable<DateTime> EnumerateDates(DateTime start, DateTime end)
        {
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
                yield return date;
        }
    }
}
