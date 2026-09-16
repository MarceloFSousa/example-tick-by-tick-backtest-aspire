using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Domain.DLL.Business;
using Domain.DLL.Business.Interfaces;
using Domain.DLL.Models;
using Domain.DLL.Settings;
using Domain.DLL.Utils;
using Domain.Spode.Exceptions;
using Domain.Spode.Models;
using Domain.Spode.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Domain.DLL.Services
{
    public class DLLService : DLLFunctions
    {
        private readonly ILogger<DLLService> _logger;
        private string _exchange;
        private DLLAuthParams _auth;
        private double _takeProfitPoints;
        private double _stopLossPoints;
        private double _entryId;
        public AccountDetails _account;
        public Dictionary<string, string> _dicOrders = new Dictionary<string, string>();

        // Agent name cache: ID → name resolved via GetAgentName
        // (calling the DLL can be expensive, especially in hot paths like building snapshots)
        private readonly ConcurrentDictionary<int, string> _agentNameCache = new();

        public DLLService(ILogger<DLLService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resolves the agent (broker) name from the ID. Uses a cache to avoid
        /// repeated calls to the DLL. On failure, returns the ID as a string.
        /// </summary>
        /// <param name="shortName">true = abbreviated, false = full (default).</param>
        // Cuts at the first "garbage" char — null, control chars or a stray CJK char
        // (the DLL sometimes leaves residual bytes after the actual name)
        private static string Sanitize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            int end = raw.Length;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c == '\0' || char.IsControl(c))
                {
                    end = i;
                    break;
                }
                // CJK range / odd symbols that show up as garbage (Itaú, JP Morgan, etc.
                // use legitimate Latin accents, but chars >= U+0400 don't make sense here)
                if (c >= 'Ѐ')
                {
                    end = i;
                    break;
                }
            }
            return raw.Substring(0, end).TrimEnd();
        }

        public string GetAgentName(int agentId, bool shortName = true)
        {
            // shortName is part of the key so we cache full vs abbreviated separately, but
            // since in practice only one mode is used, we keep it simple and cache by ID + flag.
            var key = shortName ? -agentId : agentId; // negative = short, positive = full
            return _agentNameCache.GetOrAdd(key, _ =>
            {
                try
                {
                    int flag = shortName ? 1 : 0;
                    int len = GetAgentNameLength(agentId, flag);
                    if (len <= 0) return agentId.ToString();

                    // The DLL's official sample uses StringBuilder(len) + passes len as a_AgentLen
                    var sb = new StringBuilder(len);
                    int ret = GetAgentName(len, agentId, sb, flag);
                    if (ret != NL_OK)
                    {
                        _logger.LogWarning("GetAgentName({Id}) retornou erro {Ret}", agentId, ret);
                        return agentId.ToString();
                    }

                    // Defensive: trim null/garbage that the DLL sometimes leaves in the buffer
                    // (some names came with stray CJK characters at the end).
                    return Sanitize(sb.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha em GetAgentName({Id})", agentId);
                    return agentId.ToString();
                }
            });
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

        public void SubscribeAsset(string asset)
        {
            sbyte retVal;

            retVal = (sbyte)SubscribeTicker(asset, _exchange);

            if (retVal != NL_OK)
            {
                throw new Exception($"Erro no subscribe do ativo: {retVal}");
            }

            _logger.LogInformation("Subscribe no ativo");
        }

        public void UnsubscribeAsset(string asset)
        {
            sbyte retVal;

            retVal = (sbyte)UnsubscribeTicker(asset, _exchange);

            if (retVal != NL_OK)
            {
                throw new Exception($"Erro no unsubscribe do ativo: {retVal}");
            }

            _logger.LogInformation("Ubsubscribe no ativo");
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

        public List<TConnectorTradingAccountOut> GetAccounts()
        {
            var accountsReturn = new List<TConnectorTradingAccountOut>();
            int count = GetAccountCount();
            if (count > 0)
            {
                TConnectorAccountIdentifierOut[] accounts = new TConnectorAccountIdentifierOut[count];

                int size = GetAccounts(0, 0, count, accounts);

                for (int i = 0; i < size; i++)
                {
                    var acc = new TConnectorAccountIdentifier()
                    {
                        Version = 0,
                        BrokerID = accounts[i].BrokerID,
                        AccountID = new string(accounts[i].AccountID)
                    };

                    var accOut = GetAccountAllDetails(acc);
                    accountsReturn.Add(accOut);
                }

                return accountsReturn;
            }
            else
            {
                throw new AccountNotFoundException();
            }
        }

        private TConnectorTradingAccountOut GetAccountAllDetails(TConnectorAccountIdentifier acc)
        {
            var accOut = new TConnectorTradingAccountOut() { AccountID = acc, Version = 1 };

            if (GetAccountDetails(ref accOut) != NL_OK)
            {
                throw new AccountNotFoundException();
            }

            accOut.BrokerName = new string(' ', accOut.BrokerNameLength);
            accOut.OwnerName = new string(' ', accOut.OwnerNameLength);
            accOut.SubOwnerName = new string(' ', accOut.SubOwnerNameLength);

            if (GetAccountDetails(ref accOut) != NL_OK)
            {
                throw new AccountNotFoundException();
            }

            return accOut;
        }

        public TSendOrderResult SendOrder(TConnectorSendOrder order)
        {
            var res = SendOrder(ref order);
            if (res < 0) // Order send error
            {
                _logger.LogError($"Erro para enviar ordem a mercado: {(NResult)res}");
            }
            else if (res > 0)
            {
                _entryId = res;
                _logger.LogInformation($"Ordem a mercado enviada. ID: {res}");
            }
            return new TSendOrderResult() { Result = (NResult)res, LocalId = res, IsSuccessful = res > 0 };
        }

        public void SendMarketOrder(string asset, long quantity, ETradeSide side, AccountDetails account, double takeProfitPoints, double stopLossPoints)
        {
            if (side == ETradeSide.None || side == ETradeSide.Close)
                return;

            _account = account;

            var sendOrder = new TConnectorSendOrder()
            {
                AccountID = account.ToAccountIdentifier(),
                AssetID = new TConnectorAssetIdentifier()
                {
                    Version = 0,
                    Exchange = _exchange,
                    Ticker = asset,
                    FeedType = 0
                },
                Version = 1,
                OrderType = TConnectorOrderType.Market,
                Price = -1,
                StopPrice = -1,
                Quantity = quantity,
                MessageID = -1,
                OrderSide = side == ETradeSide.Buy
                    ? TConnectorOrderSide.Buy
                    : TConnectorOrderSide.Sell,
                Password = _auth.RoutingPassword
            };

            _stopLossPoints = stopLossPoints;
            _takeProfitPoints = takeProfitPoints;

            var res = SendOrder(ref sendOrder);
            if (res < 0) // Order send error
            {
                _logger.LogError($"Erro para enviar ordem a mercado: {(NResult)res}");
            }
            else if (res > 0)
            {
                _entryId = res;
                _logger.LogInformation($"Ordem a mercado enviada. ID: {res}");
            }
        }

        public void SendTakeStop(EPositionSide side, double averagePrice, string asset, long quantity, AccountDetails account)
        {
            if (_takeProfitPoints <= 0 && _stopLossPoints <= 0)
                return;

            var oppositeSide = side == EPositionSide.Buy
                ? TConnectorOrderSide.Sell
                : TConnectorOrderSide.Buy;

            double stopPrice;
            double limitPrice;

            if (side == EPositionSide.Buy)
            {
                stopPrice = averagePrice - _stopLossPoints;
                limitPrice = averagePrice + _takeProfitPoints;
            }
            else
            {
                stopPrice = averagePrice + _stopLossPoints;
                limitPrice = averagePrice - _takeProfitPoints;
            }

            // STOP ORDER
            var stopOrder = new TConnectorSendOrder()
            {
                AccountID = account.ToAccountIdentifier(),
                AssetID = new TConnectorAssetIdentifier() { Exchange = _exchange, Ticker = asset, Version = 0, FeedType = 0 },
                Version = 1,
                OrderType = TConnectorOrderType.Stop,
                Price = stopPrice,
                StopPrice = stopPrice,
                Quantity = quantity,
                OrderSide = oppositeSide,
                Password = _auth.RoutingPassword
            };
            if (_stopLossPoints > 0)
            {
                _stopLossPoints = 0;
                var res = SendOrder(ref stopOrder);
                if (res < 0) // Order send error
                {
                    _logger.LogError($"Erro para enviar Stop: {(NResult)res}");
                }
                else if (res > 0)
                {
                    _dicOrders[$"SL:{_entryId}"] = res.ToString();
                }
            }

            // LIMIT ORDER
            var limitOrder = new TConnectorSendOrder()
            {
                AccountID = account.ToAccountIdentifier(),
                AssetID = new TConnectorAssetIdentifier() { Exchange = _exchange, Ticker = asset, Version = 0, FeedType = 0 },
                Version = 1,
                OrderType = TConnectorOrderType.Limit,
                Price = limitPrice,
                StopPrice = -1,
                Quantity = quantity,
                OrderSide = oppositeSide,
                Password = _auth.RoutingPassword
            };
            if (_takeProfitPoints > 0)
            {
                _takeProfitPoints = 0;
                var res = SendOrder(ref limitOrder);
                if (res < 0) // Order send error
                {
                    _logger.LogError($"Erro para enviar Limit: {(NResult)res}");
                }
                else if (res > 0)
                {
                    _dicOrders[$"TP:{_entryId}"] = res.ToString();
                }
            }
        }

        public TConnectorTradingAccountPosition GetPosition(string asset, TConnectorAccountIdentifier account)
        {
            var position = new TConnectorTradingAccountPosition()
            {
                AccountID = account,
                AssetID = new TConnectorAssetIdentifier() { Version = 0, Exchange = _exchange, Ticker = asset, FeedType = 0 },
                Version = 0
            };
            var res = GetPositionV2(ref position);
            return position;
        }

        public static bool GetOrderAllDetails(TConnectorOrderIdentifier orderId, out TConnectorOrderOut order)
        {
            order = new TConnectorOrderOut()
            {
                Version = 0,
                OrderID = orderId
            };

            if (GetOrderDetails(ref order) != NL_OK) { return false; }

            order.AssetID.Ticker = new string(' ', order.AssetID.TickerLength);
            order.AssetID.Exchange = new string(' ', order.AssetID.ExchangeLength);
            order.TextMessage = new string(' ', order.TextMessageLength);

            if (GetOrderDetails(ref order) != NL_OK) { return false; }

            return true;
        }
        public List<TConnectorOrder> GetAllOrders(TConnectorAccountIdentifier account)
        {
            var orders = new List<TConnectorOrder>();

            bool EnumOrders([In] in TConnectorOrder a_Order, nint a_Param)
            {
                orders.Add(a_Order);
                return true;
            }

            var accountId = account;

            var ret = EnumerateAllOrders(ref accountId, 0, 0, EnumOrders);

            if (ret != NL_OK)
            {
                _logger.LogError($"Erro para enumerar ordens: {(NResult)ret}");
            }
            else
                _logger.LogInformation($"Ordens recebidas: {orders.Count}");

            return orders;
        }

        public List<TConnectorOrder> GetAllOrdersByInterval(TConnectorAccountIdentifier account, DateTime start, DateTime end)
        {

            var orders = new List<TConnectorOrder>();

            bool EnumOrders([In] in TConnectorOrder a_Order, nint a_Param)
            {
                orders.Add(a_Order);
                return true;
            }

            var accountId = account;

            var ret = EnumerateOrdersByInterval(ref accountId, 0,
                SystemTime.FromDateTime(start), SystemTime.FromDateTime(end),
                0, EnumOrders);

            if (ret != NL_OK)
            {
                _logger.LogError($"Erro para enumerar ordens: {(NResult)ret}");
            }
            else
                _logger.LogInformation($"Ordens recebidas: {orders.Count} | Data: {start} - {end}");

            return orders;
        }
        public List<TConnectorOrder> GetAllOrdersByCurrentDay(TConnectorAccountIdentifier account)
        {

            var orders = new List<TConnectorOrder>();

            bool EnumOrders([In] in TConnectorOrder a_Order, nint a_Param)
            {
                orders.Add(a_Order);
                return true;
            }

            var start = DateTime.Now.Date;
            var end = DateTime.Now;

            var accountId = account;

            var ret = EnumerateOrdersByInterval(ref accountId, 0,
                SystemTime.FromDateTime(start), SystemTime.FromDateTime(end),
                0, EnumOrders);

            if (ret != NL_OK)
            {
                _logger.LogError($"Erro para enumerar ordens: {(NResult)ret}");
            }
            else
                _logger.LogInformation($"Ordens recebidas: {orders.Count} | Data: {start} - {end}");

            return orders;
        }


        public NResult ClosePositions(string asset, TConnectorAccountIdentifier account)
        {
            //_account = account;
            var closePosition = new TConnectorZeroPosition()
            {
                PositionType = TConnectorPositionType.DayTrade,
                AccountID = account,
                AssetID = new TConnectorAssetIdentifier() { Version = 0, Exchange = _exchange, Ticker = asset, FeedType = 0 },
                Version = 0,
                Price = -1,
                MessageID = -1,
                Password = _auth.RoutingPassword
            };
            var res = SendZeroPositionV2(ref closePosition);
            _logger.LogInformation($"send zero position result: {(NResult)res} | messageId: {closePosition.MessageID}");
            return (NResult)res;
        }

        public NResult CancelOrders(string asset, TConnectorAccountIdentifier account)
        {
            //_account = account;
            var cancelOrders = new TConnectorCancelOrders()
            {
                AccountID = account,
                AssetID = new TConnectorAssetIdentifier() { Version = 0, Exchange = _exchange, Ticker = asset, FeedType = 0 },
                Version = 0,
                Password = _auth.RoutingPassword
            };
            var res = SendCancelOrdersV2(ref cancelOrders);
            _logger.LogInformation($"send cancel orders result: {(NResult)res}");
            return (NResult)res;
        }
        public NResult CancelOrder(TConnectorAccountIdentifier account, long localId)
        {
            //_account = account;
            var cancelOrder = new TConnectorCancelOrder()
            {
                AccountID = account,
                Version = 0,
                Password = _auth.RoutingPassword,
                OrderID = new TConnectorOrderIdentifier() { LocalOrderID = localId }
            };
            var res = SendCancelOrderV2(ref cancelOrder);
            _logger.LogInformation($"send cancel order result: {(NResult)res}");
            return (NResult)res;
        }
    }
}
