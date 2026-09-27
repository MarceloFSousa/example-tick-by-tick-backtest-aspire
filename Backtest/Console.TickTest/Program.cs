using Application.TickTest.Handlers;
using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Business.Services;
using Infrastructure.TickTest.Options;
using Infrastructure.TickTest.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleApp.TickTest
{
    // Composition root of the Backtest context: wires the DI container and runs one
    // backtest. Parameters come from the args (--ticker, --exchange, --start, --end)
    // or, when an arg is missing, from Backtest in appsettings.json.
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Backtest:Storage"));

            var storageProvider = builder.Configuration["Backtest:Storage:Provider"] ?? "Parquet";
            if (string.Equals(storageProvider, "Csv", StringComparison.OrdinalIgnoreCase))
            {
                builder.Services.AddSingleton<ITradeTickRepository, CsvTradeTickRepository>();
            }
            else
            {
                builder.Services.AddSingleton<ITradeTickRepository, ParquetTradeTickRepository>();
            }

            builder.Services.AddSingleton<IBacktestCore, BacktestCore>();
            builder.Services.AddSingleton<IBacktestHandler, BacktestHandler>();

            if (!BacktestConsole.TryReadRequest(builder.Configuration, out var request, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine(BacktestConsole.Usage);
                return 1;
            }

            using var host = builder.Build();
            var handler = host.Services.GetRequiredService<IBacktestHandler>();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                var result = await handler.HandleAsync(request, cts.Token);
                BacktestConsole.PrintResult(request, result, Console.Out);
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.Error.WriteLine("Backtest cancelado.");
                return 130;
            }
        }
    }
}
