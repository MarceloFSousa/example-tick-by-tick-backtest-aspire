using System.Text.Json.Serialization;
using Application.TickTest.Handlers;
using Application.TickTest.Services;
using Application.TickTest.Workers;
using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Business.Services;
using Infrastructure.TickTest.Options;
using Infrastructure.TickTest.Persistence;

namespace Web.TickTest;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        // The domain models are structs with public fields, which System.Text.Json skips by default.
        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.IncludeFields = true;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

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

        builder.Services.AddSingleton<IBacktestReportRepository, JsonBacktestReportRepository>();
        builder.Services.AddSingleton<IBacktestCore, BacktestCore>();
        builder.Services.AddSingleton<IBacktestHandler, BacktestHandler>();
        builder.Services.AddSingleton<IBacktestJobService, BacktestJobService>();
        builder.Services.AddHostedService<BacktestWorker>();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
