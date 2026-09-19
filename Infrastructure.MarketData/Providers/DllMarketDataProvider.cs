using System;
using Domain.DLL.Services;
using Domain.DLL.Settings;
using Domain.MarketData.Business.Interfaces;
using Domain.MarketData.Models;
using Infrastructure.MarketData.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.MarketData.Providers
{
    public class DllMarketDataProvider : IMarketDataProvider
    {
        private readonly DLLService _dllService;
        private readonly MarketDataCallbacks _callbacks;
        private readonly ILogger<DllMarketDataProvider> _logger;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public event Action<TradeTick>? OnDataReceived;

        public DllMarketDataProvider(
            ILogger<DllMarketDataProvider> logger,
            DLLService dllService,
            MarketDataCallbacks callbacks,
            IOptions<DllCredentialsOptions> options)
        {
            _logger = logger;
            _dllService = dllService;
            _callbacks = callbacks;
            _callbacks.TickReceived += tick => OnDataReceived?.Invoke(tick);

            var credentials = options.Value;
            _dllService.Init(_callbacks, new DLLAuthParams
            {
                Key = credentials.Key,
                User = credentials.User,
                Password = credentials.Password,
                RoutingPassword = credentials.RoutingPassword
            }, credentials.Exchange);
        }

        public void Connect()
        {
            _dllService.Connect();
            _isConnected = true;
        }

        public void Disconnect()
        {
            // Domain.DLL wraps ProfitDLL64.dll, which has no native disconnect/finalize
            // export - this is local-only cleanup, the underlying DLL session is not
            // actually torn down.
            _isConnected = false;
            _logger.LogWarning("Disconnect() called, but the underlying DLL has no native disconnect API - the session is not actually torn down.");
        }

        public void SubscribeHistoricalData(Asset asset, DateTime start, DateTime end)
        {
            _dllService.GetHistory(start, end, asset.Ticker);
        }
    }
}
