using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Microsoft.Extensions.Logging;

namespace Application.MarketData.Services
{
    // Requests historical ticks from the provider only for days that aren't already
    // stored. Existing days are skipped so they aren't fetched (and appended) twice;
    // consecutive missing days are merged into a single provider request.
    public class MarketDataService : IMarketDataService
    {
        private readonly IMarketDataProvider _provider;
        private readonly ITradeTickRepository _repository;
        private readonly ILogger<MarketDataService> _logger;

        public MarketDataService(IMarketDataProvider provider, ITradeTickRepository repository, ILogger<MarketDataService> logger)
        {
            _provider = provider;
            _repository = repository;
            _logger = logger;
        }

        public async Task<HistoricalRequestResult> RequestHistoricalDataAsync(Asset asset, DateTime start, DateTime end, CancellationToken cancellationToken = default)
        {
            var skippedDays = new List<DateTime>();
            var ranges = new List<DateRange>();
            DateTime? runFirst = null;
            DateTime? runLast = null;

            void CloseRun()
            {
                if (runFirst is null || runLast is null)
                    return;

                // Keep the caller's exact bounds on the first/last day of the request.
                var rangeStart = runFirst.Value == start.Date ? start : runFirst.Value;
                var rangeEnd = runLast.Value == end.Date ? end : runLast.Value.AddDays(1).AddMilliseconds(-1);
                ranges.Add(new DateRange(rangeStart, rangeEnd));
                runFirst = null;
                runLast = null;
            }

            for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            {
                if (await _repository.ExistsAsync(asset.Ticker, asset.Exchange, day, cancellationToken))
                {
                    skippedDays.Add(day);
                    CloseRun();
                }
                else
                {
                    runFirst ??= day;
                    runLast = day;
                }
            }

            CloseRun();

            _logger.LogInformation(
                "Historical request for {Ticker}: {Requested} range(s) requested, {Skipped} day(s) already stored",
                asset.Ticker, ranges.Count, skippedDays.Count);

            foreach (var range in ranges)
                _provider.SubscribeHistoricalData(asset, range.Start, range.End);

            return new HistoricalRequestResult(ranges, skippedDays);
        }
    }
}
