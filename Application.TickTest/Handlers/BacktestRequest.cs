using Domain.TickTest.Models;

namespace Application.TickTest.Handlers
{
    public record BacktestRequest(Asset Asset, DateTime Start, DateTime End);
}
