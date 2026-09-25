using System;
using System.Collections.Generic;

namespace Domain.MarketData.Models
{
    public struct HistoricalRequestResult
    {
        public IReadOnlyList<DateTime> RequestedDays;
        public IReadOnlyList<DateTime> SkippedDays;

        public override string ToString() => $"Requested:{RequestedDays?.Count ?? 0} Skipped:{SkippedDays?.Count ?? 0}";
    }
}
