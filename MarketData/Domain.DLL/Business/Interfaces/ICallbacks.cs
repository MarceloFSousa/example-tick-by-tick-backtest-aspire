using Domain.DLL.Models;
using System.Collections.Concurrent;

namespace Domain.DLL.Business.Interfaces
{
    public interface ICallbacks
    {
        bool Connected { get; set; }
        bool Activated { get; set; }
        void OnStateChanged(int nResult, int result);
        void OnNewTrade(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit);
        void OnNewDay(TAssetID TAssetIDRec, string date, double sOpen, double sHigh, double sLow, double sClose, double sVol, double sAdjustment, double sMaxLimit, double sMinLimit, double sVolBuyer, double sVolSeller, int nQtd, int nDeals, int nOpenContracts, int nQtdBuyer, int nQtdSeller, int nDealsBuyer, int nDealsSeller);
        void OnNewHistory(TAssetID assetId, string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType);
        void OnProgress(TAssetID assetId, int nProgress);
        void OnOfferBookTop(TAssetID assetId, double price, int qtd, int side);
        void OnPriceBook(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nCount, double sPrice, nint pArraySell, nint pArrayBuy);
        void OnOfferBook(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, string date, nint pArraySell, nint pArrayBuy);
        void OnAssetListInfo(TAssetID assetId, string strName, string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, string validityDate, string strISIN);
        void OnAssetListInfoV2(TAssetID assetId, string strName, string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, string validityDate, string strISIN, string strSector, string strSubSector, string strSegment);
        void OnOrderCallback(TConnectorOrderIdentifier order);
        void OnPositionCallback(TConnectorAccountIdentifier AccountID, TConnectorAssetIdentifier assetId, int nEventID);
        void OnQuoteChanged(string ticker, double price);
    }
}
