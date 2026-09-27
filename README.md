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
- Any config key can be overridden, e.g. `--Backtest:Storage:RootPath=D:\Data`.
- `Backtest:Storage:RootPath` must point at the same folder MarketData writes to (default `C:\MarketDataStore`).
- Exit codes: `0` ok, `1` invalid args, `130` canceled (Ctrl+C).

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
| `Application.TickTest` | `BacktestHandler` (replays ticks day by day) |
| `Infrastructure.TickTest` | Parquet/CSV repositories (read side) |
| `Console.TickTest` | Backtest composition root and entry point |
| `Web.TickTest` | Empty scaffold (future backtest API) |
| `Tests.TickTest` | xUnit tests for the Backtest context |

See [`CLAUDE.md`](CLAUDE.md) for architecture details and conventions.

## To do

- [ ] **Backtest API** - implement `Web.TickTest` (endpoint to start a backtest and return the result) on top of `IBacktestHandler`.
- [ ] **Contexts talking to each other** - let Backtest ask the MarketData API for missing days instead of requiring the files to be there beforehand (keeping the contexts decoupled, e.g. via HTTP client behind a port).
- [ ] **Slippage** - model price slippage on fills.
- [ ] **Latency to entry** - delay order execution by a configurable latency (fill on a later tick).
- [ ] **Take profit / stop loss** - built-in TP/SL exits attached to a position or order.
- [ ] **Better backtest report** - win rate, drawdown, profit factor, per-day/per-trade breakdown, equity curve, export (CSV/JSON).
- [ ] **Frontend** - UI to request data, configure/run backtests and visualize results.
