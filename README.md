**English** | [Português (BR)](README.pt-BR.md)

# TickTest

Tick-by-tick backtesting on .NET 8 / .NET Aspire. Two independent bounded contexts:

- **MarketData** - asks a broker DLL (ProfitDLL) for historical trades and stores them on disk (Parquet or CSV).
- **Backtest** - replays the stored ticks, one at a time, through a strategy and prints the result.

The two contexts do not reference each other. They only share the **files on disk** (`{RootPath}/{Exchange}/{Ticker}/{yyyy-MM-dd}.parquet|csv`).

## Requirements

- .NET 8 SDK
- Windows with the native ProfitDLL at `C:\ProfitDLL\ProfitDLL64.dll` (only needed by MarketData)
- Broker credentials (activation key, user, password, routing password)

## How to run

The flow is: **start MarketData -> ask for data -> wait for the data -> run the backtest.**

### 1. Configure MarketData

`MarketData/Web.MarketData/appsettings.json` is gitignored because it holds real credentials. On a fresh clone:

```bash
cp MarketData/Web.MarketData/appsettings.example.json MarketData/Web.MarketData/appsettings.json
```

Then fill `MarketData:DllCredentials` (`Key`, `User`, `Password`, `RoutingPassword`, `Exchange`) and `MarketData:Storage` (`Provider` = `Parquet` or `Csv`, `RootPath`). Never commit this file.

### 2. Start MarketData

```bash
dotnet run --project MarketData/Web.MarketData
```

Swagger opens at `http://localhost:5245/swagger`.

### 3. Ask for data

Call the historical endpoint (from Swagger or any HTTP client):

```bash
curl -X POST http://localhost:5245/api/marketdata/historical -H "Content-Type: application/json" -d '{"ticker":"WINFUT","exchange":"F","start":"2026-09-15T00:00:00","end":"2026-09-18T23:59:59"}'
```

- Days already stored are skipped; the DLL is called once per missing day.
- `200 OK` means everything in the range was already on disk. `202 Accepted` means the request was sent and the ticks will arrive **asynchronously**.

### 4. Wait for the data

The response returns before the ticks are stored. Incoming ticks are buffered and flushed to disk **every 15 seconds** (and on shutdown), so:

- Watch the MarketData logs, and check that `{RootPath}/{Exchange}/{Ticker}/` has one file per day with data.
- Do not stop MarketData right after the request; wait for the last flush (or stop it gracefully so the shutdown flush runs).
- Only `Buyer`/`Seller` ticks are stored while `MarketData:Ingestion:IgnoreNonAggression` is `true` (default).

### 5. Run the backtest

Stop `Web.MarketData` first (a running web project locks the DLLs and the build fails with MSB3027). Then:

```bash
dotnet run --project Backtest/Console.TickTest
```

Parameters come from the `Backtest` section of `Backtest/Console.TickTest/appsettings.json` (`Ticker`, `Exchange`, `Start`, `End`, `Storage`). Any argument overrides it:

```bash
dotnet run --project Backtest/Console.TickTest -- --ticker WINFUT --exchange F --start 2026-09-15 --end 2026-09-18
```

- Dates are `yyyy-MM-dd`; a date-only `--end` means the end of that day.
- `--cost 1.5` (or `Backtest:CostPerContract` in the appsettings, default `0`) is the cost charged **per contract on every fill** (entry and exit), in the same unit as the PnL.
- Any config key can be overridden, e.g. `--Backtest:Storage:RootPath=D:\Data`.
- `Backtest:Storage:RootPath` must point at the same folder MarketData writes to (default `C:\MarketDataStore`).
- When the run ends, a report file is saved as `{Backtest:Reports:RootPath}/{id}.json` (default `C:\BacktestReports`) and its path is printed (`Relatório: ...`).
- Exit codes: `0` ok, `1` invalid args, `2` backtest failed, `130` canceled (Ctrl+C).

The result (printed and saved in the report) brings these statistics, all computed from the closed positions:

| Field | Meaning |
| --- | --- |
| `GrossPnL` | Sum of the trades' PnL, before costs |
| `Costs` | Filled contracts x cost per contract |
| `RealizedPnL` | `GrossPnL - Costs` |
| `NumberOfTrades` | Closed positions |
| `WinRate` | Winning trades / trades (0 to 1) |
| `ProfitFactor` | Sum of the winning trades / sum of the losing trades (`0` when no trade lost) |
| `PayOff` | `RealizedPnL / NumberOfTrades` (average net result per trade) |

A trade wins or loses by its own PnL, before costs. The position still open at the end is not included.

### 6. Or run the backtest through the API

`Web.TickTest` exposes the same backtest over HTTP. A run can take a while, so the API does not wait for it: it answers with a **process id**, runs in the background and saves a **report file** when it ends.

```bash
dotnet run --project Backtest/Web.TickTest
```

Swagger: `http://localhost:5001/swagger`. Start a backtest (parameters in the body):

```bash
curl -X POST http://localhost:5001/api/backtest \
  -H "Content-Type: application/json" \
  -d '{ "ticker": "WINFUT", "exchange": "F", "start": "2026-09-15", "end": "2026-09-18", "costPerContract": 1.5 }'
```

`costPerContract` is optional: without it the API uses `Backtest:CostPerContract` from its appsettings (default `0`). A negative value is a `400`.

It answers `202 Accepted` with the job (`id`, `status: "Pending"`) and a `Location` header. Follow it with the id:

```bash
curl http://localhost:5001/api/backtest/{id}
```

- `status` goes `Pending` -> `Running` -> `Completed` (or `Failed` / `Canceled`). While it runs `report` is `null`; when it ends the answer carries the full report (processed/skipped days, tick count, realized PnL, open position, closed positions, orders).
- The report is also saved as `{Backtest:Reports:RootPath}/{id}.json` (default `C:\BacktestReports`), for failed and canceled runs too (`error` says why).
- `400` when `ticker`/`exchange` is blank or `start` is after `end`; `404` for an unknown id. A date-only `end` means the end of that day.
- Storage and report folders come from `Backtest:Storage` and `Backtest:Reports` in `Backtest/Web.TickTest/appsettings.json`.
- One backtest runs at a time; the others wait as `Pending`. Jobs are kept in memory: restarting the API loses the pending/running ones (finished ones are still answered from their report file).

## Where to define the signal (strategy)

The strategy lives in [`BacktestCore`](Backtest/Domain.TickTest/Business/Services/BacktestCore.cs), method `Run`. It is called **once per tick** and must **return the updated `BacktestContext`**:

```csharp
context = TradeService.VerifyOpenOrders(context);   // fill pending Limit/Stop orders first
var tick = TradeRules.GetCurrentTick(context);      // current tick = context.Ticks[^1]
// ... your signal ...
context = TradeService.OpenPosition(context, EPositionSide.Long, 1);
return context;
```

Important rules:

- `context.Ticks` is chronological: the **current tick is the last one** (`Ticks[^1]`); the earlier ones are the history. Never use `Ticks[0]`.
- `BacktestContext` is a struct passed by value. **Always keep the returned value** of every `TradeService` call (`context = TradeService.X(context, ...)`), or the open position is lost.
- Available operations: `SendOrder`, `CancelOrder`, `OpenPosition`, `ClosePosition`, `VerifyOpenOrders`.

The built-in strategy is deliberately simple: a tick with `Quantity > 500` is a signal in the direction of its aggressor (`Buyer` = buy, `Seller` = sell). Flat, it opens 1 contract; holding the opposite side, it reverses.

### Tests will fail if you change the code

`Tests.TickTest` asserts the current strategy and the trade rules (`BacktestCoreTests`, `BacktestHandlerTests`, `TradeServiceTests`, ...). If you change `BacktestCore` (or `TradeService`/`TradeRules`), tests **will fail** - update them together with the code:

```bash
dotnet test Backtest/Tests.TickTest
```

### Better: create another strategy instead of editing the core

Keep `BacktestCore` (and its tests) as the reference and add a new implementation of `IBacktestCore`:

1. Create a class, e.g. `Backtest/Domain.TickTest/Business/Services/MyStrategyCore.cs`, implementing `IBacktestCore` (`BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default)`).
2. In [`Backtest/Console.TickTest/Program.cs`](Backtest/Console.TickTest/Program.cs), swap the registration:

   ```csharp
   builder.Services.AddSingleton<IBacktestCore, MyStrategyCore>();   // was BacktestCore
   ```

   (same line in [`Backtest/Web.TickTest/Program.cs`](Backtest/Web.TickTest/Program.cs) if you run it through the API)

3. Add tests for the new class (use `Support/ContextBuilder`; test names follow `MethodUnderTest_Scenario_ExpectedBehavior`).

## Repository layout

```
MarketData/   Domain.MarketData, Application.MarketData, Infrastructure.MarketData, Domain.DLL, Web.MarketData
Backtest/     Domain.TickTest, Application.TickTest, Infrastructure.TickTest, Console.TickTest, Web.TickTest, Tests.TickTest
TickTest.AppHost, TickTest.ServiceDefaults   (Aspire, shared, at the root)
```

## Solution layout

| Project | Role |
| --- | --- |
| `Domain.MarketData` | MarketData models and ports |
| `Application.MarketData` | `MarketDataService`, `MarketDataWorker` |
| `Infrastructure.MarketData` | DLL adapter, Parquet/CSV repositories |
| `Domain.DLL` | P/Invoke wrapper over ProfitDLL |
| `Web.MarketData` | MarketData composition root and API |
| `Domain.TickTest` | Backtest models, `BacktestCore`, `TradeService`, `TradeRules` |
| `Application.TickTest` | `BacktestHandler` (replays ticks day by day), `BacktestJobService` + `BacktestWorker` (background runs for the API) |
| `Infrastructure.TickTest` | Parquet/CSV repositories (read side), JSON report repository |
| `Console.TickTest` | Backtest composition root and entry point |
| `Web.TickTest` | Backtest API (`POST /api/backtest`, `GET /api/backtest/{id}`) |
| `Tests.TickTest` | xUnit tests for the Backtest context |

See [`CLAUDE.md`](CLAUDE.md) for architecture details and conventions.

## To do

- [x] **Backtest API** - `Web.TickTest` starts a backtest (`POST /api/backtest`), answers with a process id and saves a report file when it ends.
- [ ] **Contexts talking to each other** - let Backtest ask the MarketData API for missing days instead of requiring the files to be there beforehand (keeping the contexts decoupled, e.g. via HTTP client behind a port).
- [ ] **Slippage** - model price slippage on fills.
- [ ] **Latency to entry** - delay order execution by a configurable latency (fill on a later tick).
- [ ] **Take profit / stop loss** - built-in TP/SL exits attached to a position or order.
- [ ] **Better backtest report** - win rate, drawdown, profit factor, per-day/per-trade breakdown, equity curve, export (CSV/JSON).
- [ ] **Frontend** - UI to request data, configure/run backtests and visualize results.
