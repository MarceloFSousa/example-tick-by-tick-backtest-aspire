using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Microsoft.AspNetCore.Mvc;

namespace Web.MarketData.Controllers
{
    [ApiController]
    [Route("api/marketdata")]
    public class MarketDataController : ControllerBase
    {
        private readonly IMarketDataProvider _provider;

        public MarketDataController(IMarketDataProvider provider)
        {
            _provider = provider;
        }

        public record HistoricalDataRequest(string Ticker, string Exchange, DateTime Start, DateTime End);

        [HttpPost("historical")]
        public IActionResult RequestHistorical([FromBody] HistoricalDataRequest request)
        {
            _provider.SubscribeHistoricalData(new Asset { Ticker = request.Ticker, Exchange = request.Exchange }, request.Start, request.End);
            // Ticks land asynchronously via OnDataReceived -> MarketDataWorker -> repository.
            return Accepted();
        }
    }
}
