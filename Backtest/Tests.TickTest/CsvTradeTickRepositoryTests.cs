using Domain.TickTest.Models;
using Infrastructure.TickTest.Options;
using Infrastructure.TickTest.Persistence;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class CsvTradeTickRepositoryTests : IDisposable
    {
        private const string Header = "Id,Ticker,Exchange,TimestampUtc,Price,Quantity,Type,BuyerId,SellerId";

        private readonly string _root = Path.Combine(Path.GetTempPath(), "TickTestCsv_" + Guid.NewGuid());
        private readonly CsvTradeTickRepository _repository;

        public CsvTradeTickRepositoryTests()
        {
            _repository = new CsvTradeTickRepository(Microsoft.Extensions.Options.Options.Create(new StorageOptions { RootPath = _root }));
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private string DayFile => Path.Combine(_root, TestAsset.Exchange, TestAsset.Ticker, $"{T0:yyyy-MM-dd}.csv");

        private static TradeTick Tick(int second, double price = 100) => new()
        {
            Asset = TestAsset,
            Timestamp = T0.AddSeconds(second),
            Price = price,
            Quantity = 1,
            Type = ETradeType.Buyer,
            Buyer = new Agent { Id = 3, Name = "Buyer" },
            Seller = new Agent { Id = 8, Name = "Seller" }
        };

        private Task<IReadOnlyList<TradeTick>> ReadDayAsync() =>
            _repository.ReadAsync(TestAsset.Ticker, TestAsset.Exchange, T0.Date, T0.Date.AddDays(1).AddTicks(-1));

        [Fact]
        public async Task InsertRangeAsync_NewFile_AssignsSequentialIdsFromOne()
        {
            await _repository.InsertRangeAsync(new[] { Tick(0), Tick(1), Tick(2) });

            var ticks = await ReadDayAsync();

            Assert.Equal(new[] { 1, 2, 3 }, ticks.Select(t => t.Id));
        }

        [Fact]
        public async Task InsertRangeAsync_IncomingId_IsReplacedByTheSequence()
        {
            var tick = Tick(0);
            tick.Id = 99;

            await _repository.InsertRangeAsync(new[] { tick });

            Assert.Equal(1, Assert.Single(await ReadDayAsync()).Id);
        }

        [Fact]
        public async Task InsertRangeAsync_ExistingFile_ContinuesFromLastId()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DayFile)!);
            File.WriteAllLines(DayFile, new[]
            {
                Header,
                "1,WINFUT,F,01/02/2025 10:00:00,100,1,Buyer,3,8",
                "7,WINFUT,F,01/02/2025 10:00:01,100,1,Seller,3,8"
            });

            await _repository.InsertRangeAsync(new[] { Tick(2), Tick(3) });

            Assert.Equal(new[] { 1, 7, 8, 9 }, (await ReadDayAsync()).Select(t => t.Id));
        }

        [Fact]
        public async Task InsertRangeAsync_TwoAppends_WritesSingleHeader()
        {
            await _repository.InsertRangeAsync(new[] { Tick(0) });
            await _repository.InsertRangeAsync(new[] { Tick(1) });

            var lines = File.ReadAllLines(DayFile);

            Assert.Equal(3, lines.Length);
            Assert.Equal(Header, lines[0]);
            Assert.Single(lines, l => l.StartsWith("Id,"));
        }

        [Fact]
        public async Task InsertAsync_AfterRange_ReturnsNextId()
        {
            await _repository.InsertRangeAsync(new[] { Tick(0), Tick(1) });

            var id = await _repository.InsertAsync(Tick(2));

            Assert.Equal(3, id);
        }

        [Fact]
        public async Task ReadAsync_AfterAppends_ReturnsAllTicksWithEmptyAgentNames()
        {
            await _repository.InsertRangeAsync(new[] { Tick(0, 100) });
            await _repository.InsertRangeAsync(new[] { Tick(1, 105) });

            var ticks = await ReadDayAsync();

            Assert.Equal(new[] { 100d, 105d }, ticks.Select(t => t.Price));
            Assert.All(ticks, t =>
            {
                Assert.Equal(3, t.Buyer.Id);
                Assert.Equal(8, t.Seller.Id);
                Assert.True(string.IsNullOrEmpty(t.Buyer.Name));
                Assert.True(string.IsNullOrEmpty(t.Seller.Name));
            });
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_RemovesOnlyThatRow()
        {
            await _repository.InsertRangeAsync(new[] { Tick(0), Tick(1), Tick(2) });

            var removed = await _repository.DeleteAsync(2, TestAsset.Ticker, TestAsset.Exchange, T0);

            Assert.True(removed);
            Assert.Equal(new[] { 1, 3 }, (await ReadDayAsync()).Select(t => t.Id));
        }
    }
}
