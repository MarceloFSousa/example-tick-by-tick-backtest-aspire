using Application.TickTest.Handlers;
using Domain.TickTest.Models;

namespace Tests.TickTest.Support
{
    // Returns a fixed result, or throws the configured exception, and records the requests it got.
    public class FakeBacktestHandler : IBacktestHandler
    {
        public BacktestResult Result;
        public Exception? Failure;
        public List<BacktestRequest> Requests { get; } = new();

        public Task<BacktestResult> HandleAsync(BacktestRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            if (Failure is not null)
                throw Failure;
            return Task.FromResult(Result);
        }
    }
}
