using System;

namespace Domain.TickTest.Models
{
    public struct BacktestRequest
    {
        public Asset Asset;
        public DateTime Start;
        public DateTime End;
        // Cost charged per contract on every fill (entry and exit); 0 = no costs.
        public double CostPerContract;

        public override string ToString() => $"{Asset} {Start:O} -> {End:O}";
    }
}
