using System.Globalization;
using CsvHelper;
using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Infrastructure.MarketData.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.MarketData.Persistence
{
    // Same partitioning/read-whole-file/mutate/atomic-rewrite approach as
    // ParquetTradeTickRepository, with the same O(n)-per-mutation cost and coarse
    // global file lock. CsvHelper has no async file I/O, so reads/writes run
    // synchronously once the lock is acquired.
    public class CsvTradeTickRepository : ITradeTickRepository
    {
        private readonly string _rootPath;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public CsvTradeTickRepository(IOptions<StorageOptions> options)
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
                var records = ReadFile(path);
                records.Add(TradeTickRecord.FromDomain(tick));
                WriteFile(path, records);
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
                    var records = ReadFile(path);
                    records.AddRange(group.Select(TradeTickRecord.FromDomain));
                    WriteFile(path, records);
                }
                finally
                {
                    _fileLock.Release();
                }
            }
        }

        public Task<IReadOnlyList<TradeTick>> ReadAsync(string ticker, string exchange, DateTime startUtc, DateTime endUtc, CancellationToken cancellationToken = default)
        {
            var result = new List<TradeTick>();
            foreach (var date in EnumerateDates(startUtc, endUtc))
            {
                var path = GetFilePath(exchange, ticker, date);
                var records = ReadFile(path);
                result.AddRange(records
                    .Where(r => r.TimestampUtc >= startUtc && r.TimestampUtc <= endUtc)
                    .Select(r => r.ToDomain()));
            }
            return Task.FromResult<IReadOnlyList<TradeTick>>(result);
        }

        public async Task<bool> UpdateAsync(TradeTick tick, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                var path = GetFilePath(tick.Asset.Exchange, tick.Asset.Ticker, tick.Timestamp);
                var records = ReadFile(path);
                var index = records.FindIndex(r => r.Id == tick.Id);
                if (index < 0)
                    return false;

                records[index] = TradeTickRecord.FromDomain(tick);
                WriteFile(path, records);
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
                var records = ReadFile(path);
                var removed = records.RemoveAll(r => r.Id == id) > 0;
                if (removed)
                    WriteFile(path, records);
                return removed;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        private string GetFilePath(string exchange, string ticker, DateTime date) =>
            Path.Combine(_rootPath, exchange, ticker, $"{date:yyyy-MM-dd}.csv");

        private static List<TradeTickRecord> ReadFile(string path)
        {
            if (!File.Exists(path))
                return new List<TradeTickRecord>();

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<TradeTickRecord>().ToList();
        }

        private static void WriteFile(string path, List<TradeTickRecord> records)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = path + ".tmp";
            using (var writer = new StreamWriter(tempPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
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
