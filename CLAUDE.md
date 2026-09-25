# TickTest

.NET 8 / .NET Aspire solution (`TickTest.sln`) with two bounded contexts: **MarketData** (ingest ticks from a broker DLL and store them) and **Backtest** (consume stored ticks). Build with `dotnet build TickTest.sln`; a running `Web.MarketData`/`Web.TickTest` locks the DLLs and makes builds fail with file-lock errors (MSB3027) - that is not a code error.

## MarketData context (implemented)

Dependency direction - everything points at `Domain.MarketData`, never at each other:

```
Domain.MarketData  <-  Application.MarketData  <-  Web.MarketData (composition root)
Domain.MarketData  <-  Infrastructure.MarketData  <-  Web.MarketData
                              |
                              +-> Domain.DLL (only Infrastructure may reference it)
```

- `Domain.MarketData` physically lives in the **`Data.TickTest/`** folder (project name and folder differ - pre-existing, don't rename). Holds models (`TradeTick`, `Asset`, `Agent`, `ETradeType`) and the ports `IMarketDataProvider` and `ITradeTickRepository` under `Business/Interfaces/`.
- `Application.MarketData`: `MarketDataService` (`IMarketDataService`) and `MarketDataWorker` (BackgroundService).
- `Infrastructure.MarketData`: `DllMarketDataProvider` + `MarketDataCallbacks` (adapter over `Domain.DLL`), `ParquetTradeTickRepository`, `CsvTradeTickRepository`, options classes.
- `Web.MarketData`: `Program.cs` wires all DI; `MarketDataController` exposes `POST /api/marketdata/historical`.

### Data flow

`MarketDataController` -> `IMarketDataService.RequestHistoricalDataAsync` (checks `ITradeTickRepository.ExistsAsync` per day, skips stored days, calls the provider **once per missing day**) -> `IMarketDataProvider.SubscribeHistoricalData` (fire-and-forget) -> DLL callbacks (`OnNewHistory`/`OnNewTrade`) -> `MarketDataCallbacks` maps to `TradeTick` -> `OnDataReceived` -> `MarketDataWorker` (only `Buyer`/`Seller` ticks, buffered in a channel, flushed every 15s and on shutdown) -> `ITradeTickRepository.InsertRangeAsync`.

### Storage

Chosen by `MarketData:Storage:Provider` (`Parquet` default, or `Csv`). One file per asset-day: `{RootPath}/{Exchange}/{Ticker}/{yyyy-MM-dd}.parquet|csv`. Inserts **append** (Parquet via `ParquetOptions.Append`, CSV via append mode) without reading existing rows. Update/Delete must read-modify-write the whole day file. `TradeTick.Id` (Guid) is generated on insert and identifies rows for update/delete. `ExistsAsync` treats an empty file as "no data".

### Configuration

`Web.MarketData/appsettings.json` is **gitignored** (holds real broker credentials). New clones: copy `appsettings.example.json` to `appsettings.json` and fill in `MarketData:DllCredentials` (Key, User, Password, RoutingPassword, Exchange) and `MarketData:Storage`. Never commit real credentials.

## Domain.DLL (wrapper over the native ProfitDLL)

- P/Invoke into `C:\ProfitDLL\ProfitDLL64.dll` (`DLLBase.DLL_PATH`). **Do not rename `[DllImport]` extern methods in `DLLFunctions.cs`** - they bind by name to native exports (no `EntryPoint` set).
- Trimmed to market-data needs: `DLLService` only has `Init`, `Connect`, `GetHistory`. The trading/order methods were deliberately removed. There is **no native disconnect/logout** export, so `DllMarketDataProvider.Disconnect()` is local-only cleanup.
- Native calls return `int` (`NResult`, values like `0x80000003`). Never cast to `sbyte` - it truncates the code (`NL_INVALID_ARGS` showed up as "3").
- `ETradeType` mirrors `EAggressor` 1:1 (Buyer, Seller, RLP, Auction, Other); `tradeType.ToAggressor()` does the int mapping.
- Interop types (`TConnector*`, `NResult`, `SystemTime`) are in the global namespace; other models are in `Domain.DLL.Models`.
- Richer domain mapping is meant to happen in the application layer, not in `Domain.DLL`.

## Backtest context (Domain + Infrastructure implemented)

The Backtest context is **independent of MarketData by design**: it duplicates the models and storage code instead of referencing `Domain.MarketData`/`Infrastructure.MarketData`, and has no `Domain.DLL` dependency. Don't "de-duplicate" the two contexts.

- `Domain.TickTest`: `Models/` (`TradeTick`, `Asset`, `Agent`, `ETradeType` - copies of the MarketData ones in namespace `Domain.TickTest.Models`; plus `Order`, `Position`, `EOrderSide`, `EOrderType`, `EOrderStatus`) and the port `Business/Interfaces/ITradeTickRepository`. `Position.Quantity` is signed (long > 0, short < 0).
- `Infrastructure.TickTest`: `ParquetTradeTickRepository`, `CsvTradeTickRepository`, `TradeTickRecord`, `StorageOptions` - same code and same `{RootPath}/{Exchange}/{Ticker}/{yyyy-MM-dd}.parquet|csv` layout as MarketData, so Backtest reads the files MarketData wrote. `Order`/`Position` are not persisted.
- `Application.TickTest`: `Handlers/IBacktestHandler` + `BacktestHandler` - the entry point Console/Web call with a `BacktestRequest` (Asset, Start, End, defined in `Domain.TickTest/Models`). It reads `ITradeTickRepository` one day at a time (skips days with no data), orders each day's ticks by `Timestamp`, and walks them tick by tick; the engine body is still a TODO. Returns a `BacktestResult` (processed/skipped days, tick count; also in `Domain.TickTest/Models`). Not registered in DI yet.
- `Web.TickTest`, `Console.TickTest` are still empty scaffolds (don't reference Application yet). Intended shape per `Diagrama.drawio` / `Program.drawio`: Domain -> Application -> Console and API. Open: which project holds the engine logic. Update this section once decided.

## Conventions

- Identifiers and comments in English; **log and exception message strings stay in Portuguese** (leave them as they are).
- **Every model (struct/record/data class, including request/result types) lives in the `Models/` folder of its context's Domain project** (`Domain.MarketData` in `Data.TickTest/Models`, `Domain.TickTest/Models`). Never define models in `Handlers/`, `Services/`, `Persistence/` or other folders; persistence-only shapes like `TradeTickRecord` and web DTOs are the only exceptions, and stay in their own layer.
- Models are `struct`s with public fields (matches existing `Domain.DLL`/`Domain.MarketData` style). Repository/service interfaces are async with `CancellationToken`.
- Update this file when architecture changes.

## Git

- Feature work goes on branch `feature/marketdata-mining`; one commit per logical step, imperative message.
- `Program.drawio` and `.$Program.drawio.bkp` are the user's diagram files - leave them out of commits unless asked.
- Never add a `Co-Authored-By` trailer (or any attribution line) to commit messages.
