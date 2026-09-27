using ConsoleApp.TickTest;
using Domain.TickTest.Models;
using Microsoft.Extensions.Configuration;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class BacktestConsoleTests
    {
        // In-memory configuration standing in for args (--ticker ...) and appsettings.json (Backtest:...).
        private static IConfiguration Config(params (string Key, string Value)[] values) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
                .Build();

        // ---- TryReadRequest ----

        [Fact]
        public void TryReadRequest_Args_ReadsParameters()
        {
            var config = Config(("ticker", "WINFUT"), ("exchange", "F"), ("start", "2025-01-02"), ("end", "2025-01-10"));

            var ok = BacktestConsole.TryReadRequest(config, out var request, out var error);

            Assert.True(ok);
            Assert.Equal(string.Empty, error);
            Assert.Equal("WINFUT", request.Asset.Ticker);
            Assert.Equal("F", request.Asset.Exchange);
            Assert.Equal(new DateTime(2025, 1, 2), request.Start);
        }

        [Fact]
        public void TryReadRequest_OnlyAppsettings_ReadsParameters()
        {
            var config = Config(("Backtest:Ticker", "DOLFUT"), ("Backtest:Exchange", "F"),
                ("Backtest:Start", "2025-02-03"), ("Backtest:End", "2025-02-04"));

            var ok = BacktestConsole.TryReadRequest(config, out var request, out _);

            Assert.True(ok);
            Assert.Equal("DOLFUT", request.Asset.Ticker);
            Assert.Equal(new DateTime(2025, 2, 3), request.Start);
        }

        [Fact]
        public void TryReadRequest_ArgsAndAppsettings_ArgsWinPerParameter()
        {
            var config = Config(
                ("Backtest:Ticker", "DOLFUT"), ("Backtest:Exchange", "F"), ("Backtest:Start", "2025-02-03"), ("Backtest:End", "2025-02-04"),
                ("ticker", "WINFUT"), ("end", "2025-02-10"));

            var ok = BacktestConsole.TryReadRequest(config, out var request, out _);

            Assert.True(ok);
            Assert.Equal("WINFUT", request.Asset.Ticker);                 // from the arg
            Assert.Equal("F", request.Asset.Exchange);                    // from appsettings
            Assert.Equal(new DateTime(2025, 2, 3), request.Start);        // from appsettings
            Assert.Equal(2025, request.End.Year);
            Assert.Equal(10, request.End.Day);                            // from the arg
        }

        [Fact]
        public void TryReadRequest_BlankArg_FallsBackToAppsettings()
        {
            var config = Config(("ticker", "  "), ("Backtest:Ticker", "DOLFUT"), ("exchange", "F"),
                ("start", "2025-01-02"), ("end", "2025-01-03"));

            var ok = BacktestConsole.TryReadRequest(config, out var request, out _);

            Assert.True(ok);
            Assert.Equal("DOLFUT", request.Asset.Ticker);
        }

        [Theory]
        [InlineData("ticker")]
        [InlineData("exchange")]
        public void TryReadRequest_MissingTickerOrExchange_ReturnsFalse(string missing)
        {
            var values = new Dictionary<string, string>
            {
                ["ticker"] = "WINFUT", ["exchange"] = "F", ["start"] = "2025-01-02", ["end"] = "2025-01-03"
            };
            values.Remove(missing);
            var config = Config(values.Select(v => (v.Key, v.Value)).ToArray());

            var ok = BacktestConsole.TryReadRequest(config, out _, out var error);

            Assert.False(ok);
            Assert.Contains("--ticker", error);
            Assert.Contains("appsettings.json", error);
        }

        [Theory]
        [InlineData("start", null, "--start")]
        [InlineData("start", "not-a-date", "--start")]
        [InlineData("end", null, "--end")]
        [InlineData("end", "31/31/2025", "--end")]
        public void TryReadRequest_MissingOrInvalidDate_ReturnsFalse(string parameter, string? value, string expectedInMessage)
        {
            var values = new Dictionary<string, string>
            {
                ["ticker"] = "WINFUT", ["exchange"] = "F", ["start"] = "2025-01-02", ["end"] = "2025-01-03"
            };
            if (value == null) values.Remove(parameter); else values[parameter] = value;
            var config = Config(values.Select(v => (v.Key, v.Value)).ToArray());

            var ok = BacktestConsole.TryReadRequest(config, out _, out var error);

            Assert.False(ok);
            Assert.Contains(expectedInMessage, error);
            Assert.Contains("appsettings.json", error);
        }

        [Fact]
        public void TryReadRequest_DateOnlyEnd_ExtendsToEndOfDay()
        {
            var config = Config(("ticker", "WINFUT"), ("exchange", "F"), ("start", "2025-01-02"), ("end", "2025-01-10"));

            BacktestConsole.TryReadRequest(config, out var request, out _);

            Assert.Equal(new DateTime(2025, 1, 10, 23, 59, 59, 999), request.End);
        }

        [Fact]
        public void TryReadRequest_EndWithTime_KeepsTime()
        {
            var config = Config(("ticker", "WINFUT"), ("exchange", "F"), ("start", "2025-01-02"), ("end", "2025-01-10 15:30:00"));

            BacktestConsole.TryReadRequest(config, out var request, out _);

            Assert.Equal(new DateTime(2025, 1, 10, 15, 30, 0), request.End);
        }

        [Fact]
        public void TryReadRequest_StartAfterEnd_ReturnsFalse()
        {
            var config = Config(("ticker", "WINFUT"), ("exchange", "F"), ("start", "2025-01-05"), ("end", "2025-01-02"));

            var ok = BacktestConsole.TryReadRequest(config, out _, out var error);

            Assert.False(ok);
            Assert.Contains("start", error);
        }

        // ---- GetParameter ----

        [Fact]
        public void GetParameter_ArgAppsettingsOrNothing_FollowsPriority()
        {
            Assert.Equal("arg", BacktestConsole.GetParameter(Config(("ticker", "arg"), ("Backtest:Ticker", "file")), "Ticker"));
            Assert.Equal("file", BacktestConsole.GetParameter(Config(("Backtest:Ticker", "file")), "Ticker"));
            Assert.Null(BacktestConsole.GetParameter(Config(), "Ticker"));
        }

        // ---- TryParseDate ----

        [Theory]
        [InlineData("2025-01-02", true)]
        [InlineData("2025-01-02 10:30:00", true)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void TryParseDate_Value_AcceptsOnlyValidDates(string? value, bool expected)
        {
            Assert.Equal(expected, BacktestConsole.TryParseDate(value, out _));
        }

        // ---- PrintResult ----

        private static BacktestRequest SampleRequest() => new()
        {
            Asset = TestAsset,
            Start = new DateTime(2025, 1, 2),
            End = new DateTime(2025, 1, 4, 23, 59, 59)
        };

        private static BacktestResult SampleResult() => new()
        {
            ProcessedDays = new List<DateTime> { new(2025, 1, 2), new(2025, 1, 4) },
            SkippedDays = new List<DateTime> { new(2025, 1, 3) },
            TickCount = 5,
            RealizedPnL = 1234.5,
            OpenPosition = default,
            ClosedPositions = new List<ClosedPosition> { new() { Asset = TestAsset, Side = EPositionSide.Long, Quantity = 1, EntryPrice = 100, ExitPrice = 110 } },
            Orders = new List<Order> { new() { Asset = TestAsset, Side = EOrderSide.Buy, Type = EOrderType.Market, Quantity = 1 } }
        };

        [Fact]
        public void PrintResult_SampleResult_WritesSummary()
        {
            var writer = new StringWriter();

            BacktestConsole.PrintResult(SampleRequest(), SampleResult(), writer);

            var text = writer.ToString();
            Assert.Contains("Backtest WINFUT:F", text);
            Assert.Contains("Dias processados: 2", text);
            Assert.Contains("Dias sem dados: 1 (2025-01-03)", text);
            Assert.Contains("Ticks: 5", text);
            Assert.Contains("PnL realizado: 1,234.50", text);
            Assert.Contains("Posição aberta: nenhuma", text);
            Assert.Contains("Posições fechadas: 1", text);
            Assert.Contains("Ordens: 1", text);
        }

        [Fact]
        public void PrintResult_OpenPosition_ListsIt()
        {
            var result = SampleResult();
            result.OpenPosition = new Position { Asset = TestAsset, Side = EPositionSide.Short, Quantity = 2, AveragePrice = 100 };
            var writer = new StringWriter();

            BacktestConsole.PrintResult(SampleRequest(), result, writer);

            var text = writer.ToString();
            Assert.DoesNotContain("nenhuma", text);
            Assert.Contains("Short", text);
        }

        [Fact]
        public void PrintResult_NoSkippedDays_OmitsDatesList()
        {
            var result = SampleResult();
            result.SkippedDays = new List<DateTime>();
            var writer = new StringWriter();

            BacktestConsole.PrintResult(SampleRequest(), result, writer);

            Assert.Contains("Dias sem dados: 0", writer.ToString());
            Assert.DoesNotContain("Dias sem dados: 0 (", writer.ToString());
        }

        // ---- Usage ----

        [Fact]
        public void Usage_Always_MentionsArgsAndAppsettings()
        {
            Assert.Contains("--ticker", BacktestConsole.Usage);
            Assert.Contains("appsettings.json", BacktestConsole.Usage);
        }
    }
}
