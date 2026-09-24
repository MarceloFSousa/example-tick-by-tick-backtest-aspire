using Application.MarketData.Services;
using Domain.MarketData.Models;
using Microsoft.AspNetCore.Mvc;
using Web.MarketData.DTOs;

namespace Web.MarketData.Controllers
{
    [ApiController]
    [Route("api/marketdata")]
    public partial class MarketDataController : ControllerBase
    {
        private readonly IMarketDataService _marketDataService;

        public MarketDataController(IMarketDataService marketDataService)
        {
            _marketDataService = marketDataService;
        }

        [HttpPost("historical")]
        public async Task<IActionResult> RequestHistorical([FromBody] HistoricalDataRequest request, CancellationToken cancellationToken)
        {
            var asset = new Asset { Ticker = request.Ticker, Exchange = request.Exchange };
            var result = await _marketDataService.RequestHistoricalDataAsync(asset, request.Start, request.End, cancellationToken);

            // Nothing new to fetch: everything in the range is already stored.
            if (result.RequestedRanges.Count == 0)
                return Ok(result);

            // Ticks land asynchronously via OnDataReceived -> MarketDataWorker -> repository.
            return Accepted(result);
        }
    }
}
