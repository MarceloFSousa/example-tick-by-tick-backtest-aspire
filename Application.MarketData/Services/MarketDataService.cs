using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Microsoft.Extensions.Logging;

namespace Application.MarketData.Services
{
    // Requests historical ticks from the provider one day at a time, only for days
    // that aren't already stored, so existing days aren't fetched (and appended) twice.
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
            var requestedDays = new List<DateTime>();
            var skippedDays = new List<DateTime>();

            for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
            {
                if (await _repository.ExistsAsync(asset.Ticker, asset.Exchange, day, cancellationToken))
                {
                    skippedDays.Add(day);
                    continue;
                }

                // Keep the caller's exact bounds on the first/last day of the request.
                var dayStart = day == start.Date ? start : day;
                var dayEnd = day == end.Date ? end : day.AddDays(1).AddMilliseconds(-1);

                _provider.SubscribeHistoricalData(asset, dayStart, dayEnd);
                requestedDays.Add(day);
            }

            _logger.LogInformation(
                "Historical request for {Ticker}: {Requested} day(s) requested, {Skipped} day(s) already stored",
                asset.Ticker, requestedDays.Count, skippedDays.Count);

            return new HistoricalRequestResult(requestedDays, skippedDays);
        }
    }
}
