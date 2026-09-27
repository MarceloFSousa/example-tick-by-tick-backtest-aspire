using Domain.DLL.Business;
using Domain.DLL.Models;
using System.Runtime.InteropServices;

namespace Domain.DLL.Extensions
{
    public static class Util
    {
        public static EAggressor ToAggressor(this int type)
        {
            switch (type)
            {
                case 2:
                    return EAggressor.Buyer;
                case 3:
                    return EAggressor.Seller;
                case 13:
                    return EAggressor.RLP;
                case 4:
                    return EAggressor.Auction;
                default:
                    return EAggressor.Other;
            }
        }
    }
}
