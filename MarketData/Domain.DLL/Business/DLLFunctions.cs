using Domain.DLL.Models;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Domain.DLL.Business
{
    public class DLLFunctions : DLLBase
    {
        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int DLLInitializeMarketLogin(
            [MarshalAs(UnmanagedType.LPWStr)] string activationKey,
            [MarshalAs(UnmanagedType.LPWStr)] string user,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            TStateCallback stateCallback,
            TTradeCallback newTradeCallback,
            TNewDailyCallback newDailyCallback,
            TPriceBookCallback priceBookCallback,
            TOfferBookCallback offerBookCallback,
            THistoryTradeCallback newHistoryCallback,
            TProgressCallBack progressCallBack,
            TNewTinyBookCallBack newTinyBookCallBack);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int DLLInitializeLogin(
            [MarshalAs(UnmanagedType.LPWStr)] string activationKey,
            [MarshalAs(UnmanagedType.LPWStr)] string user,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            TStateCallback stateCallback,
            THistoryCallBack historyCallBack,
            TOrderChangeCallBack orderChangeCallBack,
            TAccountCallback accountCallback,
            TTradeCallback newTradeCallback,
            TNewDailyCallback newDailyCallback,
            TPriceBookCallback priceBookCallback,
            TOfferBookCallback offerBookCallback,
            THistoryTradeCallback newHistoryCallback,
            TProgressCallBack progressCallBack,
            TNewTinyBookCallBack newTinyBookCallBack);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetServerAndPort(
            [MarshalAs(UnmanagedType.LPWStr)] string strServer,
            [MarshalAs(UnmanagedType.LPWStr)] string strPort);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetServerClock(
            ref double serverClock,
            ref int nYear, ref int nMonth, ref int nDay, ref int nHour, ref int nMin, ref int nSec, ref int nMilisec);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetLastDailyClose(
            [MarshalAs(UnmanagedType.LPWStr)] string strTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string strExchange,
            ref double dClose,
            int bAdjusted);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern IntPtr GetPosition(
            [MarshalAs(UnmanagedType.LPWStr)] string accountID,
            [MarshalAs(UnmanagedType.LPWStr)] string brokerId,
            [MarshalAs(UnmanagedType.LPWStr)] string ticker,
            [MarshalAs(UnmanagedType.LPWStr)] string bolsa);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetChangeCotationCallback(TChangeCotation a_ChangeCotation);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetAssetListCallback(TAssetListCallback AssetListCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetOfferBookCallbackV2(TOfferBookCallback OfferBookCallbackV2);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetAssetListInfoCallback(TAssetListInfoCallback AssetListInfoCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetAssetListInfoCallbackV2(TAssetListInfoCallbackV2 AssetListInfoCallbackV2);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetInvalidTickerCallback(TInvalidTickerCallback InvalidTickerCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetChangeStateTickerCallback(TChangeStateTickerCallback a_changeStateTickerCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetTheoreticalPriceCallback(TTheoreticalPriceCallback a_theoreticalPriceCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetAdjustHistoryCallbackV2(TAdjustHistoryCallbackV2 AdjustHistoryCallbackV2);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetAssetPositionListCallback(TConnectorAssetPositionListCallback AssetPositionListCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetOrderChangeCallbackV2(TOrderChangeCallBackV2 OrderChangeCallbackV2);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetOrderCallback(TConnectorOrderCallback orderCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetOrderHistoryCallback(TConnectorAccountCallback orderHistoryCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetTradeCallbackV2(TConnectorTradeCallback a_TradeCallbackV2);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetHistoryTradeCallbackV2(TConnectorTradeCallback a_HistoryTradeCallbackV2);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetPriceDepthCallback(TConnectorPriceDepthCallback a_PriceDepthCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetBrokerAccountListChangedCallback(TConnectorBrokerAccountListCallback a_BrokerAccountListCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetBrokerSubAccountListChangedCallback(TConnectorBrokerSubAccountListCallback a_BrokerSubAccountListCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetTradingMessageResultCallback(TConnectorTradingMessageResultCallback a_ResultCallback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SetEnabledLogToDebug(int bEnabled);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SubscribeTicker(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int UnsubscribeTicker(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SubscribePriceBook(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int UnsubscribePriceBook(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SubscribeOfferBook(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int UnsubscribeOfferBook(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SubscribeAdjustHistory(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetHistoryTrades(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange,
            [MarshalAs(UnmanagedType.LPWStr)] string dtDateStart,
            [MarshalAs(UnmanagedType.LPWStr)] string dtDateEnd);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int FreePointer(IntPtr pointer, int nSize);


        ////////////////////////////////////////////////////////////////////////////////
        // Routing
        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern Int64 SendStopBuyOrder(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange,
            double sPrice, double sStopPrice, int nAmount);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern Int64 SendStopSellOrder(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange,
            double sPrice, double sStopPrice, int nAmount);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendChangeOrder(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcClOrdID,
            double sPrice, int nAmount);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendCancelOrder(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcClOrdID,
            [MarshalAs(UnmanagedType.LPWStr)] string password);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendCancelAllOrders(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string password);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendCancelOrders(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern Int64 SendZeroPosition(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcTicker,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcExchange,
            [MarshalAs(UnmanagedType.LPWStr)] string password,
            double sPrice);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern long SendBuyOrder(
            [MarshalAs(UnmanagedType.LPWStr)] string IDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string BrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string Password,
            [MarshalAs(UnmanagedType.LPWStr)] string Ticker,
            [MarshalAs(UnmanagedType.LPWStr)] string Exchange,
            double sPrice, int nAmount);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern long SendSellOrder(
            [MarshalAs(UnmanagedType.LPWStr)] string IDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string BrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string Password,
            [MarshalAs(UnmanagedType.LPWStr)] string Ticker,
            [MarshalAs(UnmanagedType.LPWStr)] string Exchange,
            double sPrice, int nAmount);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetOrder([MarshalAs(UnmanagedType.LPWStr)] string clOrdId);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetOrders(
            [MarshalAs(UnmanagedType.LPWStr)] string pwcIDAccount,
            [MarshalAs(UnmanagedType.LPWStr)] string pwcBrokerID,
            [MarshalAs(UnmanagedType.LPWStr)] string dtStart,
            [MarshalAs(UnmanagedType.LPWStr)] string dtEnd);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern long SendOrder([In] ref TConnectorSendOrder sendOrder);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendChangeOrderV2([In] ref TConnectorChangeOrder changeOrder);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendCancelOrderV2([In] ref TConnectorCancelOrder cancelOrder);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendCancelOrdersV2([In] ref TConnectorCancelOrders cancelOrders);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SendCancelAllOrdersV2([In] ref TConnectorCancelAllOrders cancelAllOrders);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern long SendZeroPositionV2([In] ref TConnectorZeroPosition zeroPosition);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAccountCount();

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAccounts(int startSource, int startDest, int count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] TConnectorAccountIdentifierOut[] accounts);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAccountDetails(ref TConnectorTradingAccountOut account);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetSubAccountCount(ref TConnectorAccountIdentifier masterAccountID);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetSubAccounts(ref TConnectorAccountIdentifier masterAccountID, int startSource, int startDest, int count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] TConnectorAccountIdentifierOut[] accounts);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetPositionV2(ref TConnectorTradingAccountPosition position);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetOrderDetails(ref TConnectorOrderOut order);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int HasOrdersInInterval([In] ref TConnectorAccountIdentifier a_AccountID, SystemTime a_dtStart, SystemTime a_dtEnd);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int EnumerateOrdersByInterval([In] ref TConnectorAccountIdentifier a_AccountID, byte a_OrderVersion, SystemTime a_dtStart, SystemTime a_dtEnd, IntPtr a_Param, TConnectorEnumerateOrdersProc a_Callback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int EnumerateAllOrders([In] ref TConnectorAccountIdentifier a_AccountID, byte a_OrderVersion, IntPtr a_Param, TConnectorEnumerateOrdersProc a_Callback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int TranslateTrade(nint a_pTrade, ref TConnectorTrade a_Trade);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAccountCountByBroker(int a_AgentID);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAccountsByBroker(int a_BrokerID, int a_startSource, int a_startDest, int a_count, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] TConnectorAccountIdentifierOut[] accounts);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAgentNameLength(int a_AgentID, int a_shortFlag);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetAgentName(int a_AgentLen, int a_AgentID, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder AgentName, int a_shortFlag);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int EnumerateAllPositionAssets([In] ref TConnectorAccountIdentifier a_AccountID, byte a_OrderVersion, IntPtr a_Param, TConnectorEnumerateAssetProc a_Callback);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int SubscribePriceDepth(in TConnectorAssetIdentifier assetID);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int UnsubscribePriceDepth(in TConnectorAssetIdentifier assetID);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetTheoreticalValues(in TConnectorAssetIdentifier assetID, out double price, out long quantity);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetPriceDepthSideCount(in TConnectorAssetIdentifier assetID, byte side);

        [DllImport(DLL_PATH, CallingConvention = CallingConvention.StdCall)]
        protected static extern int GetPriceGroup(in TConnectorAssetIdentifier assetID, byte side, int position, ref TConnectorPriceGroup priceGroup);
    }
}
