using System;
using System.Runtime.CompilerServices;
using System.Xml;
using Domain.Spode.Models;
using Domain.Spode.Models.Enums;

namespace Domain.DLL.Utils
{
    public static class Mapper
    {
        public static TConnectorAccountIdentifier ToAccountIdentifier(this AccountDetails accountDetails)
        {
            return new TConnectorAccountIdentifier() { AccountID = accountDetails.Account.ToString(), BrokerID = accountDetails.BrokerId, Reserved = 0, Version = 0, SubAccountID = "" };
        }
        public static EPositionSide ToPositionSide(this byte side)
        {
            if (side == 1)
                return EPositionSide.Buy;
            if (side == 2)
                return EPositionSide.Sell;

            return EPositionSide.None;
        }
        public static DateTime ToDateTime(this SystemTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Milliseconds);
        }
        public static EOrderType ToOrderType(this TConnectorOrderType orderType)
        {
            switch (orderType)
            {
                case TConnectorOrderType.Market:
                    return EOrderType.Market;
                case TConnectorOrderType.Limit:
                    return EOrderType.Limit;
                case TConnectorOrderType.Stop:
                    return EOrderType.Stop;
                default:
                    break;
            }
            return EOrderType.Market;
        }
        public static EOrderSide ToOrderSide(this TConnectorOrderSide orderSide)
        {
            switch (orderSide)
            {
                case TConnectorOrderSide.Sell:
                    return EOrderSide.Sell;
                case TConnectorOrderSide.Buy:
                    return EOrderSide.Buy;
                default:
                    break;
            }
            return EOrderSide.Buy;
        }
    }
}
