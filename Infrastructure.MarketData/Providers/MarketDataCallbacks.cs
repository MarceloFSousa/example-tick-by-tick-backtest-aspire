using System;
using Domain.DLL.Business;
using Domain.DLL.Extensions;
using Domain.DLL.Models;
using Domain.MarketData.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.MarketData.Providers
{
    // Subclasses CallbacksBase (which already implements every ICallbacks member with
    // a default try/catch-log body) and overrides only the two callbacks that produce
    // a trade tick: live trades and historical replay.
    public class MarketDataCallbacks : CallbacksBase
    {
        public event Action<TradeTick>? TickReceived;

        public MarketDataCallbacks(ILogger<CallbacksBase> logger) : base(logger)
        {
        }

        public override void OnNewTrade(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit)
        {
            //base.OnNewTrade(assetId, date, tradeNumber, price, vol, qtd, buyAgent, sellAgent, tradeType, bIsEdit);
            TickReceived?.Invoke(Map(assetId, date, price, qtd, buyAgent, sellAgent, tradeType));
        }

        public override void OnNewHistory(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType)
        {
            //base.OnNewHistory(assetId, date, tradeNumber, price, vol, qtd, buyAgent, sellAgent, tradeType);
            TickReceived?.Invoke(Map(assetId, date, price, qtd, buyAgent, sellAgent, tradeType));
        }

        private static TradeTick Map(TAssetID assetId, string date, double price, int qtd, int buyAgent, int sellAgent, int tradeType) => new()
        {
            Id = Guid.NewGuid(),
            Asset = new Asset { Ticker = assetId.Ticker, Exchange = assetId.Exchange },
            Timestamp = DateTime.Parse(date),
            Price = price,
            Quantity = qtd,
            Type = (ETradeType)(int)tradeType.ToAggressor(),
            Buyer = new Agent { Id = buyAgent },
            Seller = new Agent { Id = sellAgent }
        };
    }
}
