namespace Application.TickTest.Handlers
{
    public interface IBacktestHandler
    {
        Task<BacktestResult> HandleAsync(BacktestRequest request, CancellationToken cancellationToken = default);
    }
}
