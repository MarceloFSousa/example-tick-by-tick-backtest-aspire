using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Infrastructure.TickTest.Options;
using Microsoft.Extensions.Options;
using Parquet;
using Parquet.Serialization;

namespace Infrastructure.TickTest.Persistence
{
    // Insert/InsertRange use ParquetOptions.Append: Parquet.Net opens the existing
    // file, seeks past the last row group, writes the new batch as its own row
    // group, and rewrites only the (small) footer - the existing rows are never
    // read or re-serialized. Update/Delete can't do this: Parquet has no row-level
    // edit within an existing row group, so those still read the whole asset-day
    // file, mutate in memory, and atomically swap it in. A single global lock
    // serializes all file mutations across every asset/day; simple and safe,
    // coarser than strictly necessary.
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

            var path = GetFilePath(tick.Asset.Exchange, tick.Asset.Ticker, tick.Timestamp);

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                await AppendAsync(path, new List<TradeTickRecord> { TradeTickRecord.FromDomain(tick) }, cancellationToken);
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

            // Grouped by asset-day so each file gets one appended row group per
            // flush, instead of one append per tick.
            var groups = ticksList.GroupBy(t => (t.Asset.Exchange, t.Asset.Ticker, Date: t.Timestamp.Date));

            await _fileLock.WaitAsync(cancellationToken);
            try
            {
                foreach (var group in groups)
                {
                    var path = GetFilePath(group.Key.Exchange, group.Key.Ticker, group.Key.Date);
                    await AppendAsync(path, group.Select(TradeTickRecord.FromDomain).ToList(), cancellationToken);
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

        private static async Task AppendAsync(string path, List<TradeTickRecord> records, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var append = File.Exists(path);

            await using var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            await ParquetSerializer.SerializeAsync(records, stream, new ParquetOptions { Append = append }, cancellationToken: cancellationToken);
        }

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
