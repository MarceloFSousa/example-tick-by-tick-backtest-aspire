using Domain.MarketData.Models;

namespace Application.MarketData.Services
{
    public interface IMarketDataService
    {
        Task<HistoricalRequestResult> RequestHistoricalDataAsync(Asset asset, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    }
}
