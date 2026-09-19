using System;
using Domain.DLL.Business;
using Domain.DLL.Business.Interfaces;
using Domain.DLL.Settings;
using Microsoft.Extensions.Logging;

namespace Domain.DLL.Services
{
    public class DLLService : DLLFunctions
    {
        private readonly ILogger<DLLService> _logger;
        private string _exchange;
        private DLLAuthParams _auth;

        public DLLService(ILogger<DLLService> logger)
        {
            _logger = logger;
        }

        public void Init(ICallbacks callbacks, DLLAuthParams auth, string exchange)
        {
            SetCallbacks(callbacks);
            _auth = auth;
            _exchange = exchange.ToUpper();
        }

        public void Connect()
        {
            GC.KeepAlive(_newTinyBookCallBack);
            GC.KeepAlive(_progressCallBack);
            GC.KeepAlive(_newHistoryCallback);
            GC.KeepAlive(_offerBookCallback);
            GC.KeepAlive(_priceBookCallback);
            GC.KeepAlive(_newDailyCallback);
            GC.KeepAlive(_newTradeCallback);
            GC.KeepAlive(_historyCallBack);
            GC.KeepAlive(_orderChangeCallBack);
            GC.KeepAlive(_accountCallback);
            GC.KeepAlive(_stateCallback);
            GC.KeepAlive(_assetListInfoCallback);
            GC.KeepAlive(_assetListInfoCallbackV2);
            GC.KeepAlive(_changeCotationCallback);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            sbyte retVal;

            retVal = (sbyte)DLLInitializeLogin(_auth.Key,
                _auth.User,
                _auth.Password,
                _stateCallback,
                _historyCallBack,
                _orderChangeCallBack,
                _accountCallback,
                _newTradeCallback,
                _newDailyCallback,
                _priceBookCallback,
                _offerBookCallback,
                _newHistoryCallback,
                _progressCallBack,
                _newTinyBookCallBack);

            if (retVal != NL_OK)
            {
                throw new Exception($"Erro na inicialização: {retVal}");
            }
            else
            {
                retVal = (sbyte)SetAssetListInfoCallbackV2(_assetListInfoCallbackV2);

                if (retVal != NL_OK)
                {
                    throw new Exception($"Erro no SetAssetListInfoCallbackV2: {retVal}");
                }
                retVal = (sbyte)SetOrderCallback(_orderCallback);

                if (retVal != NL_OK)
                {
                    throw new Exception($"Erro no SetOrderCallback: {retVal}");
                }
                retVal = (sbyte)SetAssetPositionListCallback(_positionCallback);

                if (retVal != NL_OK)
                {
                    throw new Exception($"Erro no SetAssetPositionListCallback: {retVal}");
                }
                retVal = (sbyte)SetChangeCotationCallback(_changeCotationCallback);

                if (retVal != NL_OK)
                {
                    throw new Exception($"Erro no SetChangeCotationCallback: {retVal}");
                }
            }

            _logger.LogInformation("Conectando na dll");
        }

        public void GetHistory(DateTime start, DateTime end, string asset)
        {
            sbyte retVal;

            retVal = (sbyte)GetHistoryTrades(asset, _exchange, start.ToString(DATE_FORMAT), end.ToString(DATE_FORMAT));

            if (retVal != NL_OK)
            {
                throw new Exception($"Erro em carregar historico: {retVal}");
            }

            _logger.LogInformation("Requisitando historico");
        }
    }
}
