namespace Domain.DLL.Models
{
    public struct TPosition
    {
        public int BrokerID;
        public string AccountID;
        public string AccountHolder;
        public string Ticker;
        public int IntradayPosition;
        public double Price;
        public double AvgSellPrice;
        public int SellQtd;
        public double AvgBuyPrice;
        public int BuyQtd;
        public int CustodyD1;
        public int CustodyD2;
        public int CustodyD3;
        public int Blocked;
        public int Pending;
        public int Allocated;
        public int Provisioned;
        public int QtdPosition;
        public int Available;

        public override string ToString()
        {
            return $"Broker: {BrokerID}, AccountID: {AccountID}, AccountHolder: {AccountHolder}, Ticker: {Ticker}, IntradayPosition: {IntradayPosition}, Price: {Price}, AvgSellPrice: {AvgSellPrice}, AvgBuyPrice: {AvgBuyPrice}, BuyQtd: {BuyQtd}, SellQtd: {SellQtd}";
        }
    }
}
