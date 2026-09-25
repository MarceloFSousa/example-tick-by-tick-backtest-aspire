using System;
using System.Collections.Generic;

namespace Domain.TickTest.Models
{
    public struct BacktestResult
    {
        public IReadOnlyList<DateTime> ProcessedDays;
        public IReadOnlyList<DateTime> SkippedDays;
        public long TickCount;

        public override string ToString() => $"Processed:{ProcessedDays?.Count ?? 0} Skipped:{SkippedDays?.Count ?? 0} Ticks:{TickCount}";
    }
}
