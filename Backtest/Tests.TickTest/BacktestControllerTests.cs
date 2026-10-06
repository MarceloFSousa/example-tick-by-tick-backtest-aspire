using Application.TickTest.Options;
using Application.TickTest.Services;
using Domain.TickTest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.TickTest.Support;
using Web.TickTest.Controllers;
using Web.TickTest.DTOs;

namespace Tests.TickTest
{
    public class BacktestControllerTests
    {
        private static readonly DateTime Start = new(2025, 1, 2);
        private static readonly DateTime End = new(2025, 1, 3);

        private readonly FakeBacktestHandler _handler = new();
        private readonly FakeBacktestReportRepository _reports = new();
        private readonly BacktestJobService _service;
        private readonly BacktestController _controller;

        public BacktestControllerTests()
        {
            _service = new BacktestJobService(_handler, _reports, NullLogger<BacktestJobService>.Instance);
            _controller = new BacktestController(_service, Microsoft.Extensions.Options.Options.Create(new BacktestOptions { CostPerContract = 0.5 }));
        }

        [Fact]
        public void Start_ValidBody_ReturnsAcceptedWithPendingJob()
        {
            var result = _controller.Start(new BacktestApiRequest("WINFUT", "F", Start, End));

            var accepted = Assert.IsType<AcceptedAtActionResult>(result);
            var job = Assert.IsType<BacktestJob>(accepted.Value);
            Assert.NotEqual(Guid.Empty, job.Id);
            Assert.Equal(EBacktestStatus.Pending, job.Status);
            Assert.Equal(nameof(BacktestController.Get), accepted.ActionName);
            Assert.Equal(job.Id, accepted.RouteValues!["id"]);
        }

        [Fact]
        public void Start_ValidBody_MapsAssetAndStart()
        {
            var accepted = (AcceptedAtActionResult)_controller.Start(new BacktestApiRequest(" WINFUT ", "F", Start, End));

            var request = ((BacktestJob)accepted.Value!).Request;
            Assert.Equal("WINFUT", request.Asset.Ticker);
            Assert.Equal("F", request.Asset.Exchange);
            Assert.Equal(Start, request.Start);
        }

        [Fact]
        public void Start_DateOnlyEnd_UsesEndOfDay()
        {
            var accepted = (AcceptedAtActionResult)_controller.Start(new BacktestApiRequest("WINFUT", "F", Start, End));

            Assert.Equal(End.AddDays(1).AddMilliseconds(-1), ((BacktestJob)accepted.Value!).Request.End);
        }

        [Fact]
        public void Start_EndWithTime_KeepsExactEnd()
        {
            var end = End.AddHours(12);

            var accepted = (AcceptedAtActionResult)_controller.Start(new BacktestApiRequest("WINFUT", "F", Start, end));

            Assert.Equal(end, ((BacktestJob)accepted.Value!).Request.End);
        }

        [Theory]
        [InlineData("", "F")]
        [InlineData("  ", "F")]
        [InlineData("WINFUT", "")]
        public void Start_BlankTickerOrExchange_ReturnsBadRequest(string ticker, string exchange)
        {
            var result = _controller.Start(new BacktestApiRequest(ticker, exchange, Start, End));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Start_StartAfterEnd_ReturnsBadRequest()
        {
            var result = _controller.Start(new BacktestApiRequest("WINFUT", "F", End.AddDays(5), End));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Start_NoCostInBody_UsesConfiguredCost()
        {
            var accepted = (AcceptedAtActionResult)_controller.Start(new BacktestApiRequest("WINFUT", "F", Start, End));

            Assert.Equal(0.5, ((BacktestJob)accepted.Value!).Request.CostPerContract);
        }

        [Fact]
        public void Start_CostInBody_OverridesConfiguredCost()
        {
            var accepted = (AcceptedAtActionResult)_controller.Start(new BacktestApiRequest("WINFUT", "F", Start, End, 2));

            Assert.Equal(2, ((BacktestJob)accepted.Value!).Request.CostPerContract);
        }

        [Fact]
        public void Start_NegativeCost_ReturnsBadRequest()
        {
            var result = _controller.Start(new BacktestApiRequest("WINFUT", "F", Start, End, -1));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Get_UnknownId_ReturnsNotFound()
        {
            Assert.IsType<NotFoundResult>(await _controller.Get(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task Get_PendingJob_ReturnsJobWithoutReport()
        {
            var id = _service.Enqueue(new BacktestRequest { Start = Start, End = End }).Id;

            var ok = Assert.IsType<OkObjectResult>(await _controller.Get(id, CancellationToken.None));

            var response = Assert.IsType<BacktestStatusResponse>(ok.Value);
            Assert.Equal(EBacktestStatus.Pending, response.Job.Status);
            Assert.Null(response.Report);
        }

        [Fact]
        public async Task Get_CompletedJob_ReturnsJobWithReport()
        {
            _handler.Result = new BacktestResult { TickCount = 7 };
            var id = _service.Enqueue(new BacktestRequest { Start = Start, End = End }).Id;
            await _service.ProcessAsync(id);

            var ok = Assert.IsType<OkObjectResult>(await _controller.Get(id, CancellationToken.None));

            var response = Assert.IsType<BacktestStatusResponse>(ok.Value);
            Assert.Equal(EBacktestStatus.Completed, response.Job.Status);
            Assert.Equal(7, response.Report!.Value.Result.TickCount);
        }
    }
}
