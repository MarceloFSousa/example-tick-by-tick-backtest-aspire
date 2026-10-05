using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.TickTest.Business.Interfaces;
using Domain.TickTest.Models;
using Infrastructure.TickTest.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.TickTest.Persistence
{
    // One JSON file per backtest process: {RootPath}/{id}.json. Flat by id so a
    // report can be found without an index. The models are structs with public
    // fields, hence IncludeFields.
    public class JsonBacktestReportRepository : IBacktestReportRepository
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            IncludeFields = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly string _rootPath;

        public JsonBacktestReportRepository(IOptions<ReportOptions> options)
        {
            _rootPath = options.Value.RootPath;
        }

        public async Task<string> SaveAsync(BacktestReport report, CancellationToken cancellationToken = default)
        {
            var path = GetPath(report.Id);
            Directory.CreateDirectory(_rootPath);

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, report, _jsonOptions, cancellationToken);
            return path;
        }

        public async Task<BacktestReport?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var path = GetPath(id);
            if (!File.Exists(path))
                return null;

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<BacktestReport>(stream, _jsonOptions, cancellationToken);
        }

        public string GetPath(Guid id) => Path.Combine(_rootPath, $"{id}.json");
    }
}
