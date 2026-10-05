using Application.TickTest.Services;
using Microsoft.Extensions.Hosting;

namespace Application.TickTest.Workers
{
    // Runs the queued backtests one at a time (a run keeps all its ticks in
    // memory), so the API can answer with the job id without waiting for the run.
    public class BacktestWorker : BackgroundService
    {
        private readonly IBacktestJobService _jobs;

        public BacktestWorker(IBacktestJobService jobs)
        {
            _jobs = jobs;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var id in _jobs.ReadQueueAsync(stoppingToken))
                {
                    await _jobs.ProcessAsync(id, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }
    }
}
