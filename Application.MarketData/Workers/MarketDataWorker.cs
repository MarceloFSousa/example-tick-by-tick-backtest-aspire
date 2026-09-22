using System.Threading.Channels;
using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.MarketData.Workers
{
    // Bridges the connected IMarketDataProvider to persistence. Ticks raised via
    // OnDataReceived (live or historical) are buffered in memory and flushed as a
    // batch every FlushInterval, plus once more on shutdown so whatever accumulated
    // since the last flush is never dropped.
    public class MarketDataWorker : BackgroundService
    {
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);

        private readonly IMarketDataProvider _provider;
        private readonly ITradeTickRepository _repository;
        private readonly ILogger<MarketDataWorker> _logger;
        private readonly Channel<TradeTick> _channel = Channel.CreateUnbounded<TradeTick>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        private readonly object _batchLock = new();
        private List<TradeTick> _batch = new();

        public MarketDataWorker(IMarketDataProvider provider, ITradeTickRepository repository, ILogger<MarketDataWorker> logger)
        {
            _provider = provider;
            _repository = repository;
            _logger = logger;
            _provider.OnDataReceived += HandleTick;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _provider.Connect();

            var readTask = ReadIntoBatchAsync(stoppingToken);
            var flushTask = FlushOnIntervalAsync(stoppingToken);

            await Task.WhenAll(readTask, flushTask);

            // final flush for whatever accumulated between the last tick and shutdown
            await FlushBatchAsync(CancellationToken.None);
        }

        private void HandleTick(TradeTick tick)
        {
            if (tick.Type != ETradeType.Buyer && tick.Type != ETradeType.Seller)
                return;

            if (!_channel.Writer.TryWrite(tick))
                _logger.LogWarning("Failed to enqueue tick for {Ticker}", tick.Asset.Ticker);
        }

        private async Task ReadIntoBatchAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var tick in _channel.Reader.ReadAllAsync(stoppingToken))
                {
                    lock (_batchLock)
                    {
                        _batch.Add(tick);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        private async Task FlushOnIntervalAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(FlushInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await FlushBatchAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }

        private async Task FlushBatchAsync(CancellationToken cancellationToken)
        {
            List<TradeTick> toFlush;
            lock (_batchLock)
            {
                if (_batch.Count == 0)
                    return;

                toFlush = _batch;
                _batch = new List<TradeTick>();
            }

            try
            {
                await _repository.InsertRangeAsync(toFlush, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist batch of {Count} ticks", toFlush.Count);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _provider.OnDataReceived -= HandleTick;
            _provider.Disconnect();
            _channel.Writer.TryComplete();
            await base.StopAsync(cancellationToken);
        }
    }
}
