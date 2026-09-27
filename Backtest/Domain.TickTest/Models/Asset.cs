namespace Domain.TickTest.Models
{
    public struct Asset
    {
        public string Ticker;
        public string Exchange;

        public override string ToString() => $"{Ticker}:{Exchange}";
    }
}
