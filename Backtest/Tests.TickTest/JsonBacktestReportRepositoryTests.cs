using Domain.TickTest.Models;
using Domain.TickTest.Enums;
using Infrastructure.TickTest.Options;
using Infrastructure.TickTest.Persistence;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class JsonBacktestReportRepositoryTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "TickTestReports_" + Guid.NewGuid());
        private readonly JsonBacktestReportRepository _repository;

        public JsonBacktestReportRepositoryTests()
        {
            _repository = new JsonBacktestReportRepository(Microsoft.Extensions.Options.Options.Create(new ReportOptions { RootPath = _root }));
        }

        public void Dispose()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        private static BacktestReport Report(Guid id) => new()
        {
            Id = id,
            Asset = TestAsset,
            Start = new DateTime(2025, 1, 2),
            End = new DateTime(2025, 1, 3, 23, 59, 59),
            Status = EBacktestStatus.Completed,
            CreatedAt = T0,
            StartedAt = T0.AddSeconds(1),
            FinishedAt = T0.AddSeconds(5),
            Result = new BacktestResult
            {
                ProcessedDays = new List<DateTime> { new(2025, 1, 2) },
                SkippedDays = new List<DateTime> { new(2025, 1, 3) },
                TickCount = 3,
                Costs = 2.5,
                OpenPosition = new Position { Asset = TestAsset, Side = EPositionSide.Short, Quantity = 1, AveragePrice = 105, OpenAt = T0 },
                ClosedPositions = new List<ClosedPosition>
                {
                    new() { Asset = TestAsset, Side = EPositionSide.Long, Quantity = 1, EntryPrice = 100, ExitPrice = 112.5, OpenAt = T0, CloseAt = T0.AddMinutes(1) }
                },
                Orders = new List<Order>()
            }
        };

        [Fact]
        public async Task SaveAsync_NewReport_WritesFileNamedByIdAndReturnsItsPath()
        {
            var id = Guid.NewGuid();

            var path = await _repository.SaveAsync(Report(id));

            Assert.Equal(Path.Combine(_root, $"{id}.json"), path);
            Assert.Equal(_repository.GetPath(id), path);
            Assert.True(File.Exists(path));
        }

        [Fact]
        public async Task SaveAsync_NewReport_WritesFieldsAndEnumsAsText()
        {
            var id = Guid.NewGuid();

            var json = await File.ReadAllTextAsync(await _repository.SaveAsync(Report(id)));

            Assert.Contains("\"Ticker\": \"WINFUT\"", json);
            Assert.Contains("\"Status\": \"Completed\"", json);
            Assert.Contains("\"Side\": \"Short\"", json);
        }

        [Fact]
        public async Task GetAsync_SavedReport_RoundTrips()
        {
            var id = Guid.NewGuid();
            await _repository.SaveAsync(Report(id));

            var report = (await _repository.GetAsync(id))!.Value;

            Assert.Equal(id, report.Id);
            Assert.Equal(TestAsset.Ticker, report.Asset.Ticker);
            Assert.Equal(EBacktestStatus.Completed, report.Status);
            Assert.Equal(new DateTime(2025, 1, 3, 23, 59, 59), report.End);
            Assert.Equal(T0.AddSeconds(5), report.FinishedAt);
            Assert.Null(report.Error);
            Assert.Equal(3, report.Result.TickCount);
            Assert.Equal(2.5, report.Result.Costs);
            Assert.Equal(12.5, report.Result.GrossPnL);
            Assert.Equal(10, report.Result.RealizedPnL);
            Assert.Equal(new DateTime(2025, 1, 2), Assert.Single(report.Result.ProcessedDays));
            Assert.Equal(EPositionSide.Short, report.Result.OpenPosition.Side);
            Assert.Equal(112.5, Assert.Single(report.Result.ClosedPositions).ExitPrice);
            Assert.Empty(report.Result.Orders);
        }

        [Fact]
        public async Task GetAsync_MissingId_ReturnsNull()
        {
            Assert.Null(await _repository.GetAsync(Guid.NewGuid()));
        }
    }
}
