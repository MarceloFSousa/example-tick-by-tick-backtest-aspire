using System.Collections.Concurrent;
using System.Threading.Channels;
using Application.TickTest.Handlers;
using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Microsoft.Extensions.Logging;

namespace Application.TickTest.Services
{
    // Keeps the backtest processes started through the API. Jobs live in memory
    // (lost on restart) and are queued for BacktestWorker; when a job ends its
    // report is saved, so finished jobs can still be answered from the report
    // after a restart.
    public class BacktestJobService : IBacktestJobService
    {
        private readonly IBacktestHandler _handler;
        private readonly IBacktestReportRepository _reports;
        private readonly ILogger<BacktestJobService> _logger;
        private readonly ConcurrentDictionary<Guid, BacktestJob> _jobs = new();
        private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        public BacktestJobService(IBacktestHandler handler, IBacktestReportRepository reports, ILogger<BacktestJobService> logger)
        {
            _handler = handler;
            _reports = reports;
            _logger = logger;
        }

        public BacktestJob Enqueue(BacktestRequest request)
        {
            var job = new BacktestJob
            {
                Id = Guid.NewGuid(),
                Request = request,
                Status = EBacktestStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _jobs[job.Id] = job;
            _queue.Writer.TryWrite(job.Id);
            return job;
        }

        public async Task<BacktestJob?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_jobs.TryGetValue(id, out var job))
                return job;

            // Not in memory (e.g. after a restart): a finished job still has its report.
            var report = await _reports.GetAsync(id, cancellationToken);
            if (report is not { } saved)
                return null;

            return new BacktestJob
            {
                Id = saved.Id,
                Request = new BacktestRequest { Asset = saved.Asset, Start = saved.Start, End = saved.End },
                Status = saved.Status,
                CreatedAt = saved.CreatedAt,
                StartedAt = saved.StartedAt,
                FinishedAt = saved.FinishedAt,
                Error = saved.Error,
                ReportPath = _reports.GetPath(id)
            };
        }

        public Task<BacktestReport?> GetReportAsync(Guid id, CancellationToken cancellationToken = default) =>
            _reports.GetAsync(id, cancellationToken);

        public IAsyncEnumerable<Guid> ReadQueueAsync(CancellationToken cancellationToken = default) =>
            _queue.Reader.ReadAllAsync(cancellationToken);

        public async Task ProcessAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (!_jobs.TryGetValue(id, out var job))
                return;

            job.Status = EBacktestStatus.Running;
            job.StartedAt = DateTime.Now;
            _jobs[id] = job;

            var result = default(BacktestResult);
            try
            {
                result = await _handler.HandleAsync(job.Request, cancellationToken);
                job.Status = EBacktestStatus.Completed;
            }
            catch (OperationCanceledException)
            {
                job.Status = EBacktestStatus.Canceled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no backtest {Id}", id);
                job.Status = EBacktestStatus.Failed;
                job.Error = ex.Message;
            }

            job.FinishedAt = DateTime.Now;

            try
            {
                // Not the caller's token: the report must be saved even when the run was canceled.
                job.ReportPath = await _reports.SaveAsync(new BacktestReport
                {
                    Id = job.Id,
                    Asset = job.Request.Asset,
                    Start = job.Request.Start,
                    End = job.Request.End,
                    Status = job.Status,
                    CreatedAt = job.CreatedAt,
                    StartedAt = job.StartedAt,
                    FinishedAt = job.FinishedAt,
                    Error = job.Error,
                    Result = result
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao salvar o relatório do backtest {Id}", id);
                job.Status = EBacktestStatus.Failed;
                job.Error = $"Falha ao salvar o relatório: {ex.Message}";
            }

            _jobs[id] = job;
            _logger.LogInformation("Backtest {Id} finalizado: {Status}", id, job.Status);
        }
    }
}
