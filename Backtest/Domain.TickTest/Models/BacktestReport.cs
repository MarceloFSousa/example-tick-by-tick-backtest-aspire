using System;
using Domain.TickTest.Enums;

namespace Domain.TickTest.Models
{
    // What is saved when a backtest process ends (completed, failed or canceled).
    public struct BacktestReport
    {
        // Same id as the BacktestJob that produced it.
        public Guid Id;
        public Asset Asset;
        public DateTime Start;
        public DateTime End;
        public double CostPerContract;
        public EBacktestStatus Status;
        public DateTime CreatedAt;
        public DateTime? StartedAt;
        public DateTime? FinishedAt;
        // Null unless the run failed.
        public string? Error;
        // Empty when the run did not complete.
        public BacktestResult Result;

        public override string ToString() => $"{Id} {Status} {Asset} {Start:O} -> {End:O} {Result}";
    }
}
