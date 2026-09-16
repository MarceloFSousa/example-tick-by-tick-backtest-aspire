using System;

namespace Domain.DLL.Models
{
    public struct TGroupOffer
    {
        public int Qtd { get; set; }
        public Int64 OfferID { get; set; }
        public int Agent { get; set; }
        public double Price { get; set; }
        public string Date { get; set; }

        public TGroupOffer(double price, int qtd, int agent, Int64 offerId, string date)
        {
            this.Qtd = qtd;
            this.Price = price;
            this.Agent = agent;
            this.OfferID = offerId;
            this.Date = date;
        }
    };
}
