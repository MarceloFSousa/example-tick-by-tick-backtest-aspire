using Domain.MarketData.Business.Interfaces;
using Infrastructure.MarketData.Options;
using Infrastructure.MarketData.Persistence;

namespace Web.MarketData;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("MarketData:Storage"));
        builder.Services.Configure<DllCredentialsOptions>(builder.Configuration.GetSection("MarketData:DllCredentials"));

        var storageProvider = builder.Configuration["MarketData:Storage:Provider"] ?? "Parquet";
        if (string.Equals(storageProvider, "Csv", StringComparison.OrdinalIgnoreCase))
        {
            builder.Services.AddSingleton<ITradeTickRepository, CsvTradeTickRepository>();
        }
        else
        {
            builder.Services.AddSingleton<ITradeTickRepository, ParquetTradeTickRepository>();
        }

        // TODO: register MarketDataWorker + IMarketDataProvider once IMarketDataProvider
        // has a concrete implementation (deferred - see Domain.DLL/Domain.Spode gap).
        // Registering AddHostedService<MarketDataWorker>() before IMarketDataProvider
        // exists would crash the app at startup under ValidateOnBuild.

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
