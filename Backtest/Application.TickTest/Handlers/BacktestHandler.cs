using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Domain.TickTest.Enums;
using Microsoft.Extensions.Logging;

namespace Application.TickTest.Handlers
{
    // Entry point for Console/Web. Reads the stored ticks one day at a time (a day
    // can hold millions of ticks), orders each day oldest first and replays it tick
    // by tick: every tick is appended to the single context that lives for the whole
    // request (so Ticks[^1] is the current tick and the earlier ticks, from all days
    // replayed so far, are the history) and the backtest core runs once per tick.
    // The core returns the updated context, which is what the final result is built
    // from. Days without stored data are skipped and reported back to the caller.
    // Costs are charged per contract of every filled order (request.CostPerContract).
    public class BacktestHandler : IBacktestHandler
    {
        private readonly ITradeTickRepository _repository;
        private readonly IBacktestCore _core;
        private readonly ILogger<BacktestHandler> _logger;

        public BacktestHandler(ITradeTickRepository repository, IBacktestCore core, ILogger<BacktestHandler> logger)
        {
            _repository = repository;
            _core = core;
            _logger = logger;
        }

        public async Task<BacktestResult> HandleAsync(BacktestRequest request, CancellationToken cancellationToken = default)
        {
            var asset = request.Asset;
            var processedDays = new List<DateTime>();
            var skippedDays = new List<DateTime>();
            var context = new BacktestContext
            {
                Asset = asset,
                Ticks = new List<TradeTick>(),
                ClosedPositions = new List<ClosedPosition>(),
                Orders = new List<Order>()
            };
            long tickCount = 0;

            for (var day = request.Start.Date; day <= request.End.Date; day = day.AddDays(1))
            {
                if (!await _repository.ExistsAsync(asset.Ticker, asset.Exchange, day, cancellationToken))
                {
                    skippedDays.Add(day);
                    continue;
                }

                // Keep the caller's exact bounds on the first/last day of the request.
                var dayStart = day == request.Start.Date ? request.Start : day;
                var dayEnd = day == request.End.Date ? request.End : day.AddDays(1).AddMilliseconds(-1);

                // Files are appended in flushed batches, so file order is not guaranteed.
                var ticks = (await _repository.ReadAsync(asset.Ticker, asset.Exchange, dayStart, dayEnd, cancellationToken))
                    .OrderBy(t => t.Timestamp)
                    .ToList();

                foreach (var tick in ticks)
                {
                    context.Ticks.Add(tick);
                    context = _core.Run(context, cancellationToken);
                }
                tickCount += ticks.Count;

                processedDays.Add(day);
            }

            _logger.LogInformation(
                "Backtest de {Ticker}: {Processed} dia(s) processado(s), {Skipped} dia(s) sem dados, {Ticks} tick(s)",
                asset.Ticker, processedDays.Count, skippedDays.Count, tickCount);

            return new BacktestResult
            {
                ProcessedDays = processedDays,
                SkippedDays = skippedDays,
                TickCount = tickCount,
                OpenPosition = context.Position,
                ClosedPositions = context.ClosedPositions,
                Orders = context.Orders,
                Costs = context.Orders.Where(o => o.Status == EOrderStatus.Filled).Sum(o => o.Quantity) * request.CostPerContract
            };
        }
    }
}
