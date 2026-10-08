using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Infrastructure.TickTest.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.TickTest.Persistence
{
    // Insert/InsertRange open the file in append mode and write only the new rows -
    // no read of existing content needed, since CSV is just lines of text (only the
    // first append to an existing file reads it once, to find its last Id). Update/
    // Delete still need the full read-modify-write-whole-file approach (same as
    // ParquetTradeTickRepository) since a specific existing row has to be located
    // and rewritten. CsvHelper has no async file I/O, so reads/writes run
    // synchronously once the lock is acquired. A single global lock serializes all
    // file mutations across every asset/day; simple and safe, coarser than
    // strictly necessary.
    public class CsvTradeTickRepository : ITradeTickRepository
    {
        private readonly string _rootPath;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);
        private static readonly Dictionary<string, int> _lastIds = new();

        public CsvTradeTickRepository(IOptions<StorageOptions> options)
        {
            _rootPath = options.Value.RootPath;
        }

        public async Task<int> InsertAsync(TradeTick tick, CancellationToken cancellationToken = default)
        {
            var path = GetFilePath(tick.Asset.Exchange, tick.Asset.Ticker, tick.Timestamp);

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                tick.Id = GetLastId(path) + 1;
                AppendFile(path, new List<TradeTickRecord> { TradeTickRecord.FromDomain(tick) });
                _lastIds[path] = tick.Id;
            }
            finally
            {
                _fileLock.Release();
            }

            return tick.Id;
        }

        public async Task InsertRangeAsync(IEnumerable<TradeTick> ticks, CancellationToken cancellationToken = default)
        {
            // Grouped by asset-day so each file gets one append per flush, instead
            // of one append per tick.
            var groups = ticks.GroupBy(t => (t.Asset.Exchange, t.Asset.Ticker, Date: t.Timestamp.Date));

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                foreach (var group in groups)
                {
                    var path = GetFilePath(group.Key.Exchange, group.Key.Ticker, group.Key.Date);
                    var lastId = GetLastId(path);
                    var records = new List<TradeTickRecord>();
                    foreach (var tick in group)
                    {
                        var record = TradeTickRecord.FromDomain(tick);
                        record.Id = ++lastId;
                        records.Add(record);
                    }

                    AppendFile(path, records);
                    _lastIds[path] = lastId;
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public Task<bool> ExistsAsync(string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default)
        {
            // An empty file (e.g. left behind by an interrupted write) doesn't count as data.
            var file = new FileInfo(GetFilePath(exchange, ticker, dateUtc));
            return Task.FromResult(file.Exists && file.Length > 0);
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

        public async Task<bool> DeleteAsync(int id, string ticker, string exchange, DateTime dateUtc, CancellationToken cancellationToken = default)
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

        // Ids are sequential per asset-day file, like an identity column. The last
        // one is read from the file the first time it is appended to and cached
        // from then on. Call only while holding the lock.
        private static int GetLastId(string path)
        {
            if (_lastIds.TryGetValue(path, out var lastId))
                return lastId;

            var records = ReadFile(path);
            lastId = records.Count == 0 ? 0 : records.Max(r => r.Id);
            _lastIds[path] = lastId;
            return lastId;
        }

        private static void AppendFile(string path, List<TradeTickRecord> records)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // The header goes only at the top of a new file; later appends are rows only.
            var file = new FileInfo(path);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = !file.Exists || file.Length == 0
            };

            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, config);
            csv.WriteRecords(records);
        }

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
