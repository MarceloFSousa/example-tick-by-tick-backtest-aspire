using Application.TickTest.Services;
using Application.TickTest.Workers;
using Domain.TickTest.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.TickTest.Support;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class BacktestJobServiceTests
    {
        private static readonly BacktestRequest Request = new()
        {
            Asset = TestAsset,
            Start = new DateTime(2025, 1, 2),
            End = new DateTime(2025, 1, 3),
            CostPerContract = 0.5
        };

        private readonly FakeBacktestHandler _handler = new()
        {
            Result = new BacktestResult
            {
                TickCount = 42,
                ClosedPositions = new List<ClosedPosition> { new() { Side = EPositionSide.Long, Quantity = 1, EntryPrice = 100, ExitPrice = 110 } }
            }
        };
        private readonly FakeBacktestReportRepository _reports = new();

        private BacktestJobService Service() => new(_handler, _reports, NullLogger<BacktestJobService>.Instance);

        [Fact]
        public async Task Enqueue_ValidRequest_ReturnsPendingJob()
        {
            var service = Service();

            var job = service.Enqueue(Request);

            Assert.NotEqual(Guid.Empty, job.Id);
            Assert.Equal(EBacktestStatus.Pending, job.Status);
            Assert.Null(job.ReportPath);
            Assert.Equal(job.Id, (await service.GetAsync(job.Id))!.Value.Id);
        }

        [Fact]
        public async Task ProcessAsync_HandlerSucceeds_CompletesAndSavesReport()
        {
            var service = Service();
            var id = service.Enqueue(Request).Id;

            await service.ProcessAsync(id);

            var job = (await service.GetAsync(id))!.Value;
            Assert.Equal(EBacktestStatus.Completed, job.Status);
            Assert.NotNull(job.StartedAt);
            Assert.NotNull(job.FinishedAt);
            Assert.Null(job.Error);
            Assert.Equal(_reports.GetPath(id), job.ReportPath);

            var report = (await service.GetReportAsync(id))!.Value;
            Assert.Equal(EBacktestStatus.Completed, report.Status);
            Assert.Equal(TestAsset.Ticker, report.Asset.Ticker);
            Assert.Equal(Request.Start, report.Start);
            Assert.Equal(Request.End, report.End);
            Assert.Equal(0.5, report.CostPerContract);
            Assert.Equal(42, report.Result.TickCount);
            Assert.Equal(10, report.Result.RealizedPnL);
        }

        [Fact]
        public async Task ProcessAsync_HandlerThrows_FailsWithErrorAndSavesReport()
        {
            _handler.Failure = new InvalidOperationException("boom");
            var service = Service();
            var id = service.Enqueue(Request).Id;

            await service.ProcessAsync(id);

            var job = (await service.GetAsync(id))!.Value;
            Assert.Equal(EBacktestStatus.Failed, job.Status);
            Assert.Equal("boom", job.Error);
            Assert.Equal(EBacktestStatus.Failed, _reports.Reports[id].Status);
            Assert.Equal("boom", _reports.Reports[id].Error);
        }

        [Fact]
        public async Task ProcessAsync_HandlerCanceled_CancelsAndSavesReport()
        {
            _handler.Failure = new OperationCanceledException();
            var service = Service();
            var id = service.Enqueue(Request).Id;

            await service.ProcessAsync(id);

            Assert.Equal(EBacktestStatus.Canceled, (await service.GetAsync(id))!.Value.Status);
            Assert.Equal(EBacktestStatus.Canceled, _reports.Reports[id].Status);
        }

        [Fact]
        public async Task ProcessAsync_ReportSaveFails_FailsWithoutReportPath()
        {
            _reports.SaveFailure = new IOException("disco cheio");
            var service = Service();
            var id = service.Enqueue(Request).Id;

            await service.ProcessAsync(id);

            var job = (await service.GetAsync(id))!.Value;
            Assert.Equal(EBacktestStatus.Failed, job.Status);
            Assert.Contains("disco cheio", job.Error);
            Assert.Null(job.ReportPath);
        }

        [Fact]
        public async Task GetAsync_UnknownId_ReturnsNull()
        {
            Assert.Null(await Service().GetAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetAsync_NotInMemoryButReportSaved_RebuildsJobFromReport()
        {
            var id = Guid.NewGuid();
            _reports.Reports[id] = new BacktestReport
            {
                Id = id,
                Asset = TestAsset,
                Start = Request.Start,
                End = Request.End,
                Status = EBacktestStatus.Completed
            };

            var job = (await Service().GetAsync(id))!.Value;

            Assert.Equal(id, job.Id);
            Assert.Equal(EBacktestStatus.Completed, job.Status);
            Assert.Equal(TestAsset.Ticker, job.Request.Asset.Ticker);
            Assert.Equal(Request.End, job.Request.End);
            Assert.Equal(_reports.GetPath(id), job.ReportPath);
        }

        [Fact]
        public async Task ReadQueueAsync_SeveralJobs_YieldsIdsInEnqueueOrder()
        {
            var service = Service();
            var first = service.Enqueue(Request).Id;
            var second = service.Enqueue(Request).Id;

            var ids = new List<Guid>();
            await foreach (var id in service.ReadQueueAsync())
            {
                ids.Add(id);
                if (ids.Count == 2)
                    break;
            }

            Assert.Equal(new[] { first, second }, ids);
        }

        [Fact]
        public async Task ExecuteAsync_FailingJobThenGoodJob_WorkerKeepsGoing()
        {
            var service = Service();
            _handler.Failure = new InvalidOperationException("boom");
            var failed = service.Enqueue(Request).Id;
            var worker = new BacktestWorker(service);

            await worker.StartAsync(CancellationToken.None);
            await WaitForAsync(service, failed);

            _handler.Failure = null;
            var completed = service.Enqueue(Request).Id;
            await WaitForAsync(service, completed);
            await worker.StopAsync(CancellationToken.None);

            Assert.Equal(EBacktestStatus.Failed, (await service.GetAsync(failed))!.Value.Status);
            Assert.Equal(EBacktestStatus.Completed, (await service.GetAsync(completed))!.Value.Status);
        }

        private static async Task WaitForAsync(BacktestJobService service, Guid id)
        {
            for (var attempt = 0; attempt < 200; attempt++)
            {
                if ((await service.GetAsync(id))!.Value.FinishedAt is not null)
                    return;
                await Task.Delay(25);
            }
            Assert.Fail("O job não terminou a tempo.");
        }
    }
}
