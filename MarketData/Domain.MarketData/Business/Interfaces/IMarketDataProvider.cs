using Domain.MarketData.Models;

namespace Domain.MarketData.Business.Interfaces
{
    public interface IMarketDataProvider
    {
        bool IsConnected { get; }
        event Action<TradeTick> OnDataReceived;
        void Connect();
        void Disconnect();
        void SubscribeHistoricalData(Asset asset, DateTime start, DateTime end);
    }
}
