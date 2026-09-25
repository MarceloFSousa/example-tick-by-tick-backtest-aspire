using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Microsoft.Extensions.Logging;

namespace Application.TickTest.Handlers
{
    // Entry point for Console/Web. Reads the stored ticks one day at a time (a day
    // can hold millions of ticks, so the whole range is never in memory), orders
    // them newest first and hands each day to the backtest core through a single
    // context that lives for the whole request. Days without stored data are
    // skipped and reported back to the caller.
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
            var context = new BacktestContext { Asset = asset };
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
                var ticks = await _repository.ReadAsync(asset.Ticker, asset.Exchange, dayStart, dayEnd, cancellationToken);
                context.Ticks = ticks.OrderByDescending(t => t.Timestamp).ToList();

                _core.Run(context, cancellationToken);
                tickCount += context.Ticks.Count;

                processedDays.Add(day);
            }

            _logger.LogInformation(
                "Backtest de {Ticker}: {Processed} dia(s) processado(s), {Skipped} dia(s) sem dados, {Ticks} tick(s)",
                asset.Ticker, processedDays.Count, skippedDays.Count, tickCount);

            return new BacktestResult { ProcessedDays = processedDays, SkippedDays = skippedDays, TickCount = tickCount };
        }
    }
}
