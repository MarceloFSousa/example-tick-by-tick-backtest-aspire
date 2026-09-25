using Domain.DLL.Business.Interfaces;
using Domain.DLL.Extensions;
using Domain.DLL.Models;
using Domain.DLL.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Domain.DLL.Business
{
    public abstract class CallbacksBase : ICallbacks
    {
        public bool Connected { get; set; }
        public bool Activated { get; set; }

        protected readonly ILogger<CallbacksBase> _logger;

        public CallbacksBase(ILogger<CallbacksBase> logger)
        {
            _logger = logger;
        }

    public virtual void OnAssetListInfo(TAssetID assetId, string strName, string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, string validityDate, string strISIN)
        {
            try
            {
                _logger.LogInformation($"AssetListInfo: {assetId.Ticker} Tick: {sMinPriceInc}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnAssetListInfo: {ex.Message}");
            }
        }

        public virtual void OnAssetListInfoV2(TAssetID assetId, string strName, string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, string validityDate, string strISIN, string strSector, string strSubSector, string strSegment)
        {
            try
            {
                _logger.LogInformation($"AssetListInfo: {assetId.Ticker} Tick: {sMinPriceInc}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnAssetListInfoV2: {ex.Message}");
            }
        }

        public virtual void OnOfferBook(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, string date, nint pArraySell, nint pArrayBuy)
        {
            try
            {
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnOfferBook: {ex.Message}");
            }
        }

        public virtual void OnPriceBook(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nCount, double sPrice, nint pArraySell, nint pArrayBuy)
        {
            try
            {
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnPriceBook: {ex.Message}");
            }
        }

        public virtual void OnStateChanged(int nResult, int result)
        {
            try
            {
                bool connected = Connected;
                bool activated = Activated;
                _logger.LogWarning($"Mudança de estado: {StateService.Verify(nResult, result, ref connected, ref activated)}");
                Connected = connected;
                Activated = activated;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnStateChanged: {ex.Message}");
            }
        }

        public virtual void OnNewDay(TAssetID TAssetIDRec, string date, double sOpen, double sHigh, double sLow, double sClose, double sVol, double sAdjustment, double sMaxLimit, double sMinLimit, double sVolBuyer, double sVolSeller, int nQtd, int nDeals, int nOpenContracts, int nQtdBuyer, int nQtdSeller, int nDealsBuyer, int nDealsSeller)
        {
            try
            {
                _logger.LogInformation($"OnNovoDia: {TAssetIDRec.Ticker} VOC: {sVolBuyer} VOV: {sVolSeller}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnNewDay: {ex.Message}");
            }
        }

        public virtual void OnNewHistory(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType)
        {
            try
            {
                var aggressor = tradeType.ToAggressor();
                if (buyAgent != sellAgent)
                {
                    _logger.LogInformation($"Novo trade historico no {assetId.Ticker} em {price} quantidade {qtd} comprador {buyAgent} vendedor {sellAgent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnNewHistory: {ex.Message}");
            }
        }

        public virtual void OnNewTrade(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit)
        {
            try
            {
                _logger.LogInformation($"Novo trade no {assetId.Ticker} em {price} quantidade {qtd} comprador {buyAgent} vendedor {sellAgent}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnNewTrade: {ex.Message}");
            }
        }

        public virtual void OnProgress(TAssetID assetId, int nProgress)
        {
            try
            {
                _logger.LogInformation($"Progresso: {assetId.Ticker} Tick: {nProgress}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnProgress: {ex.Message}");
            }
        }

        public virtual void OnOfferBookTop(TAssetID assetId, double price, int qtd, int side)
        {
            try
            {
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnOfferBookTop: {ex.Message}");
            }
        }

        public virtual void OnOrderCallback(TConnectorOrderIdentifier order)
        {
            try
            {
                _logger.LogInformation($"Ordem id: {order.ClOrderID} Ordem localId: {order.LocalOrderID}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnOrderCallback: {ex.Message}");
            }
        }

        public virtual void OnPositionCallback(TConnectorAccountIdentifier AccountID, TConnectorAssetIdentifier assetId, int nEventID)
        {
            try
            {
                _logger.LogInformation($"Posicao atualizada em: {assetId.Ticker} Conta: {AccountID.AccountID} Broker: {AccountID.BrokerID} Event: {nEventID}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnPositionCallback: {ex.Message}");
            }
        }

        public virtual void OnQuoteChanged(string ticker, double price)
        {
            try
            {
                _logger.LogInformation($"Cotacao: {ticker} -> {price}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception no OnQuoteChanged: {ex.Message}");
            }
        }
    }
}
