using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.MarketData.Workers
{
    // Bridges the connected IMarketDataProvider to persistence: every tick raised
    // via OnDataReceived (live or historical) is handed to ITradeTickRepository.
    public class MarketDataWorker : BackgroundService
    {
        private readonly IMarketDataProvider _provider;
        private readonly ITradeTickRepository _repository;
        private readonly ILogger<MarketDataWorker> _logger;

        public MarketDataWorker(IMarketDataProvider provider, ITradeTickRepository repository, ILogger<MarketDataWorker> logger)
        {
            _provider = provider;
            _repository = repository;
            _logger = logger;
            _provider.OnDataReceived += HandleTick;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _provider.Connect();
            return Task.CompletedTask;
        }

        private async void HandleTick(TradeTick tick)
        {
            try
            {
                await _repository.InsertAsync(tick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist tick for {Ticker}", tick.Asset.Ticker);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _provider.OnDataReceived -= HandleTick;
            _provider.Disconnect();
            await base.StopAsync(cancellationToken);
        }
    }
}
