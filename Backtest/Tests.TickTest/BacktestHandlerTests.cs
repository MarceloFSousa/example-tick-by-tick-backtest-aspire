using Application.TickTest.Handlers;
using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Business.Services;
using Domain.TickTest.Models;
using Domain.TickTest.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.TickTest.Support;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class BacktestHandlerTests
    {
        private static readonly DateTime Day1 = new(2025, 1, 2);
        private static readonly DateTime Day2 = new(2025, 1, 3);
        private static readonly DateTime Day3 = new(2025, 1, 4);

        private static TradeTick Tick(DateTime time, double price = 100, double quantity = 1, ETradeType type = ETradeType.Buyer) => new()
        {
            Asset = TestAsset, Timestamp = time, Price = price, Quantity = quantity, Type = type
        };

        private static BacktestRequest Request(DateTime start, DateTime end) =>
            new() { Asset = TestAsset, Start = start, End = end.Date.AddDays(1).AddMilliseconds(-1) };

        private static BacktestHandler Handler(FakeTradeTickRepository repository, IBacktestCore core) =>
            new(repository, core, NullLogger<BacktestHandler>.Instance);

        // Records what the core sees on each call.
        private sealed class RecordingCore : IBacktestCore
        {
            public List<(DateTime Current, int Count, bool Chronological)> Calls { get; } = new();

            public BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default)
            {
                var chronological = context.Ticks.Zip(context.Ticks.Skip(1), (a, b) => a.Timestamp <= b.Timestamp).All(x => x);
                Calls.Add((context.Ticks[^1].Timestamp, context.Ticks.Count, chronological));
                return context;
            }
        }

        // Changes the open position (a struct field) and the closed list, to prove the handler keeps what the core returns.
        private sealed class PositionChangingCore : IBacktestCore
        {
            public BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default)
            {
                if (context.Ticks.Count == 1)
                    context.ClosedPositions.Add(new ClosedPosition { Asset = context.Asset, Side = EPositionSide.Long, Quantity = 1, EntryPrice = 100, ExitPrice = 105 });
                context.Position = new Position { Asset = context.Asset, Side = EPositionSide.Short, Quantity = context.Ticks.Count, AveragePrice = 100 };
                return context;
            }
        }

        [Fact]
        public async Task HandleAsync_UnsortedDay_ReplaysChronologicallyWithCurrentTickLast()
        {
            var repository = new FakeTradeTickRepository().WithDay(Day1,
                Tick(Day1.AddHours(12)), Tick(Day1.AddHours(9)), Tick(Day1.AddHours(10)));
            var core = new RecordingCore();

            await Handler(repository, core).HandleAsync(Request(Day1, Day1));

            Assert.Equal(new[] { Day1.AddHours(9), Day1.AddHours(10), Day1.AddHours(12) }, core.Calls.Select(c => c.Current));
            Assert.Equal(new[] { 1, 2, 3 }, core.Calls.Select(c => c.Count));
            Assert.All(core.Calls, c => Assert.True(c.Chronological));
        }

        [Fact]
        public async Task HandleAsync_SeveralDays_KeepsTheHistoryAcrossDays()
        {
            var repository = new FakeTradeTickRepository()
                .WithDay(Day1, Tick(Day1.AddHours(9)), Tick(Day1.AddHours(10)))
                .WithDay(Day2, Tick(Day2.AddHours(9)));
            var core = new RecordingCore();

            await Handler(repository, core).HandleAsync(Request(Day1, Day2));

            Assert.Equal(new[] { 1, 2, 3 }, core.Calls.Select(c => c.Count));
            Assert.Equal(Day2.AddHours(9), core.Calls[^1].Current);
            Assert.All(core.Calls, c => Assert.True(c.Chronological));
        }

        [Fact]
        public async Task HandleAsync_DayWithoutData_IsSkippedAndReported()
        {
            var repository = new FakeTradeTickRepository()
                .WithDay(Day1, Tick(Day1.AddHours(9)), Tick(Day1.AddHours(10)))
                .WithDay(Day3, Tick(Day3.AddHours(9)));

            var result = await Handler(repository, new RecordingCore()).HandleAsync(Request(Day1, Day3));

            Assert.Equal(new[] { Day1, Day3 }, result.ProcessedDays);
            Assert.Equal(new[] { Day2 }, result.SkippedDays);
            Assert.Equal(3, result.TickCount);
        }

        [Fact]
        public async Task HandleAsync_NoDataAtAll_ReturnsAnEmptyResultWithoutCallingTheCore()
        {
            var core = new RecordingCore();

            var result = await Handler(new FakeTradeTickRepository(), core).HandleAsync(Request(Day1, Day2));

            Assert.Empty(core.Calls);
            Assert.Empty(result.ProcessedDays);
            Assert.Equal(2, result.SkippedDays.Count);
            Assert.Equal(0, result.TickCount);
            Assert.Equal(0, result.RealizedPnL);
        }

        [Fact]
        public async Task HandleAsync_CoreResult_BuildsTheFinalResult()
        {
            var repository = new FakeTradeTickRepository().WithDay(Day1,
                Tick(Day1.AddHours(9)), Tick(Day1.AddHours(10)), Tick(Day1.AddHours(11)));

            var result = await Handler(repository, new PositionChangingCore()).HandleAsync(Request(Day1, Day1));

            // The open Position is a struct field: it only reaches the result if the handler keeps the returned context.
            Assert.Equal(EPositionSide.Short, result.OpenPosition.Side);
            Assert.Equal(3, result.OpenPosition.Quantity);
            Assert.Single(result.ClosedPositions);
            Assert.Equal(5, result.RealizedPnL);
        }

        [Fact]
        public async Task HandleAsync_RealStrategy_TradesOnTheSignalsOfTheReplayedTicks()
        {
            // Out of order in the file: 9:00 big buy, 9:30 big sell (reverses), 10:00 small tick.
            var repository = new FakeTradeTickRepository().WithDay(Day1,
                Tick(Day1.AddHours(10), price: 120, quantity: 10),
                Tick(Day1.AddHours(9), price: 100, quantity: 600, type: ETradeType.Buyer),
                Tick(Day1.AddHours(9).AddMinutes(30), price: 110, quantity: 600, type: ETradeType.Seller));

            var result = await Handler(repository, new BacktestCore()).HandleAsync(Request(Day1, Day1));

            Assert.Equal(3, result.Orders.Count);                    // open long, close, open short
            var closed = Assert.Single(result.ClosedPositions);
            Assert.Equal(10, closed.PnL);
            Assert.Equal(10, result.RealizedPnL);
            Assert.Equal(EPositionSide.Short, result.OpenPosition.Side);
            Assert.Equal(110, result.OpenPosition.AveragePrice);
        }

        [Fact]
        public async Task HandleAsync_CostPerContract_ChargesEveryFilledContract()
        {
            // Open long, close, open short: 3 filled orders of 1 contract.
            var repository = new FakeTradeTickRepository().WithDay(Day1,
                Tick(Day1.AddHours(9), price: 100, quantity: 600, type: ETradeType.Buyer),
                Tick(Day1.AddHours(9).AddMinutes(30), price: 110, quantity: 600, type: ETradeType.Seller));
            var request = Request(Day1, Day1);
            request.CostPerContract = 0.5;

            var result = await Handler(repository, new BacktestCore()).HandleAsync(request);

            Assert.Equal(1.5, result.Costs);
            Assert.Equal(10, result.GrossPnL);
            Assert.Equal(8.5, result.RealizedPnL);
        }

        [Fact]
        public async Task HandleAsync_NoCostPerContract_HasNoCosts()
        {
            var repository = new FakeTradeTickRepository().WithDay(Day1, Tick(Day1.AddHours(9), quantity: 600));

            var result = await Handler(repository, new BacktestCore()).HandleAsync(Request(Day1, Day1));

            Assert.Equal(0, result.Costs);
        }

        [Fact]
        public async Task HandleAsync_CanceledToken_Throws()
        {
            var repository = new FakeTradeTickRepository().WithDay(Day1, Tick(Day1.AddHours(9), quantity: 600));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                Handler(repository, new BacktestCore()).HandleAsync(Request(Day1, Day1), cts.Token));
        }
    }
}
