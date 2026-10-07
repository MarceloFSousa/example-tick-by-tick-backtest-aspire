using Application.TickTest.Handlers;
using Application.TickTest.Services;
using Domain.TickTest.Models;
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
    // or, when an arg is missing, from Backtest in appsettings.json. The run goes
    // through the same job service as the API, so it also saves a report file.
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // appsettings.json sits next to the executable, not in the caller's current directory.
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory
            });

            builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Backtest:Storage"));
            builder.Services.Configure<ReportOptions>(builder.Configuration.GetSection("Backtest:Reports"));

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
            builder.Services.AddSingleton<IBacktestReportRepository, JsonBacktestReportRepository>();
            builder.Services.AddSingleton<IBacktestJobService, BacktestJobService>();

            if (!BacktestConsole.TryReadRequest(builder.Configuration, out var request, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine(BacktestConsole.Usage);
                return 1;
            }

            using var host = builder.Build();
            var jobs = host.Services.GetRequiredService<IBacktestJobService>();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // Runs here instead of in BacktestWorker: the console waits for its single run.
            var id = jobs.Enqueue(request).Id;
            await jobs.ProcessAsync(id, cts.Token);
            var job = (await jobs.GetAsync(id))!.Value;

            if (job.Status == EBacktestStatus.Completed && await jobs.GetReportAsync(id) is { } report)
                BacktestConsole.PrintResult(request, report.Result, Console.Out);
            else if (job.Status == EBacktestStatus.Canceled)
                Console.Error.WriteLine("Backtest cancelado.");
            else
                Console.Error.WriteLine($"Backtest falhou: {job.Error}");

            if (job.ReportPath is not null)
                Console.Out.WriteLine($"Relatório: {job.ReportPath}");

            return job.Status switch
            {
                EBacktestStatus.Completed => 0,
                EBacktestStatus.Canceled => 130,
                _ => 2
            };
        }
    }
}
