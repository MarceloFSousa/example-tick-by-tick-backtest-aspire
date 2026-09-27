using Domain.DLL.Business.Interfaces;
using Domain.DLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Domain.DLL.Business
{
    public abstract class DLLBase
    {
        protected const string DLL_PATH = @"C:\ProfitDLL\ProfitDLL64.dll";   // @Fill in with the DLL path
        protected const sbyte NL_ERR_INIT = 80;
        protected const sbyte NL_OK = 0;
        protected const sbyte NL_ERR_INVALID_ARGS = 90;
        protected const sbyte NL_ERR_INTERNAL_ERROR = 100;
        protected const string DATE_FORMAT = "dd/MM/yyyy HH:mm:ss.fff";

        protected TStateCallback _stateCallback;
        protected THistoryCallBack _historyCallBack;
        protected TOrderChangeCallBack _orderChangeCallBack;
        protected TAccountCallback _accountCallback;
        protected TTradeCallback _newTradeCallback;
        protected TNewDailyCallback _newDailyCallback;
        protected TPriceBookCallback _priceBookCallback;
        protected TOfferBookCallback _offerBookCallback;
        protected THistoryTradeCallback _newHistoryCallback;
        protected TProgressCallBack _progressCallBack;
        protected TNewTinyBookCallBack _newTinyBookCallBack;
        protected TAssetListInfoCallback _assetListInfoCallback;
        protected TAssetListInfoCallbackV2 _assetListInfoCallbackV2;
        protected TConnectorOrderCallback _orderCallback;
        protected TConnectorAssetPositionListCallback _positionCallback;
        protected TChangeCotation _changeCotationCallback;

        public delegate void TStateCallback(int nResult, int result);
        public delegate void TNewTradeCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit);
        public delegate void TNewDailyCallback(TAssetID TAssetIDRec, [MarshalAs(UnmanagedType.LPWStr)] string date, double sOpen, double sHigh, double sLow, double sClose, double sVol, double sAdjustment, double sMaxLimit, double sMinLimit, double sVolBuyer, double sVolSeller, int nQtd, int nDeals, int nOpenContracts, int nQtdBuyer, int nQtdSeller, int nDealsBuyer, int nDealsSeller);
        public delegate void TNewHistoryCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType);
        public delegate void TProgressCallBack(TAssetID assetId, int nProgress);
        public delegate void TNewTinyBookCallBack(TAssetID assetId, double price, int qtd, int side);
        public delegate void TPriceBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nCount, double sPrice, nint pArraySell, nint pArrayBuy);
        public delegate void TOfferBookCallback(TAssetID assetId, int nAction, int nPosition, int Side, int nQtd, int nAgent, long nOfferID, double sPrice, int bHasPrice, int bHasQtd, int bHasDate, int bHasOfferID, int bHasAgent, [MarshalAs(UnmanagedType.LPWStr)] string date, nint pArraySell, nint pArrayBuy);
        public delegate void TAssetListInfoCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN);
        public delegate void TAssetListInfoCallbackV2(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName, [MarshalAs(UnmanagedType.LPWStr)] string strDescription, int nMinOrderQtd, int nMaxOrderQtd, int nLote, int stSecurityType, int ssSecuritySubType, double sMinPriceInc, double sContractMultiplier, [MarshalAs(UnmanagedType.LPWStr)] string validityDate, [MarshalAs(UnmanagedType.LPWStr)] string strISIN, [MarshalAs(UnmanagedType.LPWStr)] string strSector, [MarshalAs(UnmanagedType.LPWStr)] string strSubSector, [MarshalAs(UnmanagedType.LPWStr)] string strSegment);


        public delegate void TTradeCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType, int bIsEdit);

        public delegate void THistoryTradeCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, uint tradeNumber, double price, double vol, int qtd, int buyAgent, int sellAgent, int tradeType);

        ////////////////////////////////////////////////////////////////////////////////
        // Order change callback

        public delegate void TChangeCotation(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string date, uint tradeNumber, double sPrice);

        public delegate void TAssetListCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strName);
        public delegate void TConnectorOrderCallback(TConnectorOrderIdentifier orderId);
        public delegate void TConnectorAccountCallback(TConnectorAccountIdentifier accountId);

        public delegate void TConnectorTradeCallback(TConnectorAssetIdentifier a_Asset, nint a_pTrade, [MarshalAs(UnmanagedType.U4)] TConnectorTradeCallbackFlags a_nFlags);

        public delegate void TAdjustHistoryCallbackV2(TAssetID assetId,
            double dValue,
            [MarshalAs(UnmanagedType.LPWStr)] string adjustType,
            [MarshalAs(UnmanagedType.LPWStr)] string strObserv,
            [MarshalAs(UnmanagedType.LPWStr)] string dtAdjustment,
            [MarshalAs(UnmanagedType.LPWStr)] string dtDeliberation,
            [MarshalAs(UnmanagedType.LPWStr)] string dtPayment,
            int nFlags,
            double dMult);


        public delegate void TChangeStateTickerCallback(TAssetID assetId, [MarshalAs(UnmanagedType.LPWStr)] string strDate, int nState);

        public delegate void TInvalidTickerCallback(TConnectorAssetIdentifier assetId);

        public delegate void TTheoreticalPriceCallback(TAssetID assetId, double dTheoreticalPrice, Int64 nTheoreticalQtd);

        public delegate void TConnectorAssetPositionListCallback(TConnectorAccountIdentifier AccountID, TConnectorAssetIdentifier assetId, int nEventID);

        public delegate void THistoryCallBack(TAssetID AssetID, int nBrokerId, int nQtd, int nTradedQtd, int nLeavesQtd, int Side, double sPrice, double sStopPrice, double sAvgPrice, long nProfitID,
            [MarshalAs(UnmanagedType.LPWStr)] string OrderTypeText,
            [MarshalAs(UnmanagedType.LPWStr)] string Account,
            [MarshalAs(UnmanagedType.LPWStr)] string AccountHolder,
            [MarshalAs(UnmanagedType.LPWStr)] string ClOrdID,
            [MarshalAs(UnmanagedType.LPWStr)] string Status,
            [MarshalAs(UnmanagedType.LPWStr)] string Date);

        public delegate void TOrderChangeCallBack(TAssetID assetId, int nBrokerId, int nQtd, int nTradedQtd, int nLeavesQtd, int Side, double sPrice, double sStopPrice, double sAvgPrice, long nProfitID,
            [MarshalAs(UnmanagedType.LPWStr)] string OrderTypeText,
            [MarshalAs(UnmanagedType.LPWStr)] string Account,
            [MarshalAs(UnmanagedType.LPWStr)] string AccountHolder,
            [MarshalAs(UnmanagedType.LPWStr)] string ClOrdID,
            [MarshalAs(UnmanagedType.LPWStr)] string Status,
            [MarshalAs(UnmanagedType.LPWStr)] string Date,
            [MarshalAs(UnmanagedType.LPWStr)] string TextMessage);

        public delegate void TOrderChangeCallBackV2(TAssetID assetId, int nBrokerId, int nQtd, int nTradedQtd, int nLeavesQtd, int Side, int nValidity, double sPrice, double sStopPrice, double sAvgPrice, long nProfitID,
            [MarshalAs(UnmanagedType.LPWStr)] string OrderTypeText,
            [MarshalAs(UnmanagedType.LPWStr)] string Account,
            [MarshalAs(UnmanagedType.LPWStr)] string AccountHolder,
            [MarshalAs(UnmanagedType.LPWStr)] string ClOrdID,
            [MarshalAs(UnmanagedType.LPWStr)] string Status,
            [MarshalAs(UnmanagedType.LPWStr)] string Date,
            [MarshalAs(UnmanagedType.LPWStr)] string LastUpdate,
            [MarshalAs(UnmanagedType.LPWStr)] string CloseDate,
            [MarshalAs(UnmanagedType.LPWStr)] string ValidityDate,
            [MarshalAs(UnmanagedType.LPWStr)] string TextMessage);

        ////////////////////////////////////////////////////////////////////////////////
        // Callback with the account list
        public delegate void TAccountCallback(int nBrokerId,
            [MarshalAs(UnmanagedType.LPWStr)] string BrokerFullName,
            [MarshalAs(UnmanagedType.LPWStr)] string AccountID,
            [MarshalAs(UnmanagedType.LPWStr)] string AccountHolderName);

        public delegate void TConnectorBrokerAccountListCallback(int nBrokerId, int nChanged);

        public delegate void TConnectorBrokerSubAccountListCallback(TConnectorAccountIdentifier accountId);

        ////////////////////////////////////////////////////////////////////////////////
        // Callback with marketData information
        public delegate void TConnectorPriceDepthCallback(TConnectorAssetIdentifier a_AssetID, byte a_Side, int a_nPosition, byte a_UpdateType);

        public delegate void TConnectorTradingMessageResultCallback(in TConnectorTradingMessageResult a_pResult);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool TConnectorEnumerateOrdersProc([In] in TConnectorOrder a_Order, nint a_Param);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool TConnectorEnumerateAssetProc([In] in TConnectorAssetIdentifier a_Asset, nint a_Param);

        protected void SetCallbacks(ICallbacks callbacks)
        {
            _stateCallback = new(callbacks.OnStateChanged);
            _newTradeCallback = new(callbacks.OnNewTrade);
            _newDailyCallback = new(callbacks.OnNewDay);
            _priceBookCallback = new(callbacks.OnPriceBook);
            _offerBookCallback = new(callbacks.OnOfferBook);
            _newHistoryCallback = new(callbacks.OnNewHistory);
            _progressCallBack = new(callbacks.OnProgress);
            _newTinyBookCallBack = new(callbacks.OnOfferBookTop);
            _assetListInfoCallback = new(callbacks.OnAssetListInfo);
            _assetListInfoCallbackV2 = new(callbacks.OnAssetListInfoV2);
            _orderCallback = new(callbacks.OnOrderCallback);
            _positionCallback = new(callbacks.OnPositionCallback);
            _changeCotationCallback = new((assetId, date, tradeNumber, price) => callbacks.OnQuoteChanged(assetId.Ticker, price));
        }
    }
}
