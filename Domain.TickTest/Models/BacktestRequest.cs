using System;

namespace Domain.TickTest.Models
{
    public struct BacktestRequest
    {
        public Asset Asset;
        public DateTime Start;
        public DateTime End;

        public override string ToString() => $"{Asset} {Start:O} -> {End:O}";
    }
}
