using System;

namespace Domain.TickTest.Models
{
    // Live state of one backtest process started through the API.
    public struct BacktestJob
    {
        public Guid Id;
        public BacktestRequest Request;
        public EBacktestStatus Status;
        public DateTime CreatedAt;
        public DateTime? StartedAt;
        public DateTime? FinishedAt;
        // Null unless the run failed.
        public string? Error;
        // Null until the report file is saved.
        public string? ReportPath;

        public override string ToString() => $"{Id} {Status} {Request}";
    }
}
