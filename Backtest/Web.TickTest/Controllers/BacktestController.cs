using Application.TickTest.Services;
using Domain.TickTest.Models;
using Microsoft.AspNetCore.Mvc;
using Web.TickTest.DTOs;

namespace Web.TickTest.Controllers
{
    [ApiController]
    [Route("api/backtest")]
    public class BacktestController : ControllerBase
    {
        private readonly IBacktestJobService _jobService;

        public BacktestController(IBacktestJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpPost]
        public IActionResult Start([FromBody] BacktestApiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Ticker))
                return BadRequest("ticker é obrigatório.");

            if (string.IsNullOrWhiteSpace(request.Exchange))
                return BadRequest("exchange é obrigatório.");

            // A date-only end covers the whole day; otherwise the handler's exact end
            // bound would drop every tick of the last day.
            var end = request.End;
            if (end.TimeOfDay == TimeSpan.Zero)
                end = end.Date.AddDays(1).AddMilliseconds(-1);

            if (request.Start > end)
                return BadRequest("start deve ser menor ou igual a end.");

            var job = _jobService.Enqueue(new BacktestRequest
            {
                Asset = new Asset { Ticker = request.Ticker.Trim(), Exchange = request.Exchange.Trim() },
                Start = request.Start,
                End = end
            });

            // The run happens in the background (BacktestWorker); follow it with GET api/backtest/{id}.
            return AcceptedAtAction(nameof(Get), new { id = job.Id }, job);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        {
            if (await _jobService.GetAsync(id, cancellationToken) is not { } job)
                return NotFound();

            var report = job.ReportPath is null ? null : await _jobService.GetReportAsync(id, cancellationToken);
            return Ok(new BacktestStatusResponse(job, report));
        }
    }
}
