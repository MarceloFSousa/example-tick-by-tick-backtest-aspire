namespace Domain.DLL.Models
{
    public struct TGroupPrice
    {
        public int Qtd { get; set; }
        public int Count { get; set; }
        public double Price { get; set; }

        public TGroupPrice(double price, int count, int qtd)
        {
            this.Qtd = qtd;
            this.Price = price;
            this.Count = count;
        }
    }
}
