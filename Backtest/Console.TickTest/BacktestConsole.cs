using System.Globalization;
using Domain.TickTest.Models;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp.TickTest
{
    // Console helpers behind Program, public so they can be tested directly.
    public static class BacktestConsole
    {
        public const string Usage =
            "Uso: dotnet run --project Backtest/Console.TickTest -- --ticker WINFUT --exchange F --start 2025-01-02 --end 2025-01-10\n" +
            "  Sem argumentos, os parâmetros vêm da seção Backtest do appsettings.json (Ticker, Exchange, Start, End); argumentos têm prioridade.\n" +
            "  --start/--end: yyyy-MM-dd (um --end sem horário vai até o fim do dia)\n" +
            "  Opcional: --Backtest:Storage:Provider=Csv|Parquet  --Backtest:Storage:RootPath=<pasta>";

        // Each parameter comes from the command-line arg (--ticker) when it is given,
        // otherwise from the appsettings.json section Backtest (Backtest:Ticker).
        public static bool TryReadRequest(IConfiguration configuration, out BacktestRequest request, out string error)
        {
            request = default;

            var ticker = GetParameter(configuration, "Ticker");
            var exchange = GetParameter(configuration, "Exchange");
            if (string.IsNullOrWhiteSpace(ticker) || string.IsNullOrWhiteSpace(exchange))
            {
                error = "Informe ticker e exchange (--ticker/--exchange ou Backtest:Ticker/Backtest:Exchange no appsettings.json).";
                return false;
            }

            if (!TryParseDate(GetParameter(configuration, "Start"), out var start))
            {
                error = "start ausente ou inválido (--start ou Backtest:Start no appsettings.json).";
                return false;
            }

            if (!TryParseDate(GetParameter(configuration, "End"), out var end))
            {
                error = "end ausente ou inválido (--end ou Backtest:End no appsettings.json).";
                return false;
            }

            // A date-only end covers the whole day; otherwise the handler's exact end
            // bound would drop every tick of the last day.
            if (end.TimeOfDay == TimeSpan.Zero)
                end = end.Date.AddDays(1).AddMilliseconds(-1);

            if (start > end)
            {
                error = "start deve ser menor ou igual a end.";
                return false;
            }

            request = new BacktestRequest
            {
                Asset = new Asset { Ticker = ticker, Exchange = exchange },
                Start = start,
                End = end
            };
            error = string.Empty;
            return true;
        }

        public static string? GetParameter(IConfiguration configuration, string name)
        {
            var fromArgs = configuration[name.ToLowerInvariant()];
            return !string.IsNullOrWhiteSpace(fromArgs) ? fromArgs : configuration[$"Backtest:{name}"];
        }

        public static bool TryParseDate(string? value, out DateTime date) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

        public static void PrintResult(BacktestRequest request, BacktestResult result, TextWriter writer)
        {
            writer.WriteLine();
            writer.WriteLine($"Backtest {request.Asset} de {request.Start:yyyy-MM-dd HH:mm:ss} até {request.End:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Dias processados: {result.ProcessedDays.Count}");
            writer.WriteLine($"Dias sem dados: {result.SkippedDays.Count}" +
                (result.SkippedDays.Count > 0 ? $" ({string.Join(", ", result.SkippedDays.Select(d => d.ToString("yyyy-MM-dd")))})" : string.Empty));
            writer.WriteLine($"Ticks: {result.TickCount}");
            writer.WriteLine($"PnL realizado: {result.RealizedPnL.ToString("N2", CultureInfo.InvariantCulture)}");
            writer.WriteLine($"Posição aberta: {(result.OpenPosition.Quantity > 0 ? result.OpenPosition.ToString() : "nenhuma")}");
            writer.WriteLine($"Posições fechadas: {result.ClosedPositions.Count}");
            foreach (var closed in result.ClosedPositions)
                writer.WriteLine($"  {closed}");
            writer.WriteLine($"Ordens: {result.Orders.Count}");
            foreach (var order in result.Orders)
                writer.WriteLine($"  {order}");
        }
    }
}
