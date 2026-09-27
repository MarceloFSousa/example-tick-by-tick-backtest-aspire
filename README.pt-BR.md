[English](README.md) | **Português (BR)**

# TickTest

Backtest tick a tick em .NET 8 / .NET Aspire. Dois contextos independentes (bounded contexts):

- **MarketData** - pede trades históricos para uma DLL de corretora (ProfitDLL) e os salva em disco (Parquet ou CSV).
- **Backtest** - reproduz os ticks salvos, um de cada vez, por uma estratégia e imprime o resultado.

Os dois contextos não referenciam um ao outro. Eles só compartilham os **arquivos em disco** (`{RootPath}/{Exchange}/{Ticker}/{yyyy-MM-dd}.parquet|csv`).

## Requisitos

- .NET 8 SDK
- Windows com a ProfitDLL nativa em `C:\ProfitDLL\ProfitDLL64.dll` (necessária apenas para o MarketData)
- Credenciais da corretora (chave de ativação, usuário, senha, senha de roteamento)

## Como rodar

O fluxo é: **subir o MarketData -> pedir os dados -> esperar os dados -> rodar o backtest.**

### 1. Configurar o MarketData

`MarketData/Web.MarketData/appsettings.json` está no `.gitignore` porque contém credenciais reais. Em um clone novo:

```bash
cp MarketData/Web.MarketData/appsettings.example.json MarketData/Web.MarketData/appsettings.json
```

Depois preencha `MarketData:DllCredentials` (`Key`, `User`, `Password`, `RoutingPassword`, `Exchange`) e `MarketData:Storage` (`Provider` = `Parquet` ou `Csv`, `RootPath`). Nunca faça commit desse arquivo.

### 2. Subir o MarketData

```bash
dotnet run --project MarketData/Web.MarketData
```

O Swagger abre em `http://localhost:5245/swagger`.

### 3. Pedir os dados

Chame o endpoint de histórico (pelo Swagger ou qualquer cliente HTTP):

```bash
curl -X POST http://localhost:5245/api/marketdata/historical -H "Content-Type: application/json" -d '{"ticker":"WINFUT","exchange":"F","start":"2026-09-15T00:00:00","end":"2026-09-18T23:59:59"}'
```

- Dias já salvos são pulados; a DLL é chamada uma vez por dia faltante.
- `200 OK` significa que tudo no intervalo já estava em disco. `202 Accepted` significa que o pedido foi enviado e os ticks chegarão **de forma assíncrona**.

### 4. Esperar os dados

A resposta volta antes de os ticks serem salvos. Os ticks recebidos ficam em buffer e são gravados em disco **a cada 15 segundos** (e no shutdown), então:

- Acompanhe os logs do MarketData e confira se `{RootPath}/{Exchange}/{Ticker}/` tem um arquivo por dia com dados.
- Não derrube o MarketData logo após o pedido; espere o último flush (ou pare-o de forma graciosa para o flush de shutdown rodar).
- Só ticks `Buyer`/`Seller` são salvos enquanto `MarketData:Ingestion:IgnoreNonAggression` for `true` (padrão).

### 5. Rodar o backtest

Pare o `Web.MarketData` antes (um projeto web rodando trava as DLLs e o build falha com MSB3027). Depois:

```bash
dotnet run --project Backtest/Console.TickTest
```

Os parâmetros vêm da seção `Backtest` de `Backtest/Console.TickTest/appsettings.json` (`Ticker`, `Exchange`, `Start`, `End`, `Storage`). Qualquer argumento sobrescreve o arquivo:

```bash
dotnet run --project Backtest/Console.TickTest -- --ticker WINFUT --exchange F --start 2026-09-15 --end 2026-09-18
```

- Datas no formato `yyyy-MM-dd`; um `--end` só com data significa o fim daquele dia.
- Qualquer chave de configuração pode ser sobrescrita, ex.: `--Backtest:Storage:RootPath=D:\Data`.
- `Backtest:Storage:RootPath` deve apontar para a mesma pasta onde o MarketData grava (padrão `C:\MarketDataStore`).
- Códigos de saída: `0` ok, `1` argumentos inválidos, `130` cancelado (Ctrl+C).

## Onde definir o sinal (estratégia)

A estratégia fica em [`BacktestCore`](Backtest/Domain.TickTest/Business/Services/BacktestCore.cs), método `Run`. Ele é chamado **uma vez por tick** e deve **retornar o `BacktestContext` atualizado**:

```csharp
context = TradeService.VerifyOpenOrders(context);   // executa antes as ordens Limit/Stop pendentes
var tick = TradeRules.GetCurrentTick(context);      // tick atual = context.Ticks[^1]
// ... seu sinal ...
context = TradeService.OpenPosition(context, EPositionSide.Long, 1);
return context;
```

Regras importantes:

- `context.Ticks` é cronológico: o **tick atual é o último** (`Ticks[^1]`); os anteriores são o histórico. Nunca use `Ticks[0]`.
- `BacktestContext` é uma struct passada por valor. **Sempre guarde o valor retornado** de cada chamada ao `TradeService` (`context = TradeService.X(context, ...)`), senão a posição aberta é perdida.
- Operações disponíveis: `SendOrder`, `CancelOrder`, `OpenPosition`, `ClosePosition`, `VerifyOpenOrders`.

A estratégia embutida é propositalmente simples: um tick com `Quantity > 500` é um sinal na direção do seu agressor (`Buyer` = compra, `Seller` = venda). Zerado, abre 1 contrato; segurando o lado oposto, inverte a posição.

### Os testes vão quebrar se você mudar o código

O `Tests.TickTest` valida a estratégia atual e as regras de trade (`BacktestCoreTests`, `BacktestHandlerTests`, `TradeServiceTests`, ...). Se você mudar o `BacktestCore` (ou `TradeService`/`TradeRules`), os testes **vão falhar** - atualize-os junto com o código:

```bash
dotnet test Backtest/Tests.TickTest
```

### Melhor: crie outra estratégia em vez de editar o core

Mantenha o `BacktestCore` (e seus testes) como referência e adicione uma nova implementação de `IBacktestCore`:

1. Crie uma classe, ex.: `Backtest/Domain.TickTest/Business/Services/MyStrategyCore.cs`, implementando `IBacktestCore` (`BacktestContext Run(BacktestContext context, CancellationToken cancellationToken = default)`).
2. Em [`Backtest/Console.TickTest/Program.cs`](Backtest/Console.TickTest/Program.cs), troque o registro:

   ```csharp
   builder.Services.AddSingleton<IBacktestCore, MyStrategyCore>();   // era BacktestCore
   ```

3. Adicione testes para a nova classe (use `Support/ContextBuilder`; nomes de teste seguem `MethodUnderTest_Scenario_ExpectedBehavior`).

## Estrutura de pastas

```
MarketData/   Domain.MarketData, Application.MarketData, Infrastructure.MarketData, Domain.DLL, Web.MarketData
Backtest/     Domain.TickTest, Application.TickTest, Infrastructure.TickTest, Console.TickTest, Web.TickTest, Tests.TickTest
TickTest.AppHost, TickTest.ServiceDefaults   (Aspire, compartilhados, na raiz)
```

## Estrutura da solution

| Projeto | Papel |
| --- | --- |
| `Domain.MarketData` | Modelos e portas do MarketData |
| `Application.MarketData` | `MarketDataService`, `MarketDataWorker` |
| `Infrastructure.MarketData` | Adaptador da DLL, repositórios Parquet/CSV |
| `Domain.DLL` | Wrapper P/Invoke sobre a ProfitDLL |
| `Web.MarketData` | Composition root e API do MarketData |
| `Domain.TickTest` | Modelos do Backtest, `BacktestCore`, `TradeService`, `TradeRules` |
| `Application.TickTest` | `BacktestHandler` (reproduz os ticks dia a dia) |
| `Infrastructure.TickTest` | Repositórios Parquet/CSV (lado de leitura) |
| `Console.TickTest` | Composition root e ponto de entrada do Backtest |
| `Web.TickTest` | Scaffold vazio (futura API de backtest) |
| `Tests.TickTest` | Testes xUnit do contexto Backtest |

Veja o [`CLAUDE.md`](CLAUDE.md) para detalhes de arquitetura e convenções (em inglês).

## A fazer

- [ ] **API do Backtest** - implementar o `Web.TickTest` (endpoint para iniciar um backtest e devolver o resultado) sobre o `IBacktestHandler`.
- [ ] **Contextos conversando entre si** - fazer o Backtest pedir à API do MarketData os dias faltantes, em vez de exigir que os arquivos já existam (mantendo os contextos desacoplados, ex.: cliente HTTP atrás de uma porta).
- [ ] **Slippage** - modelar o slippage de preço nas execuções.
- [ ] **Latência de entrada** - atrasar a execução das ordens por uma latência configurável (executar em um tick posterior).
- [ ] **Take profit / stop loss** - saídas TP/SL nativas, associadas a uma posição ou ordem.
- [ ] **Relatório de backtest melhor** - taxa de acerto, drawdown, profit factor, detalhamento por dia/por trade, curva de capital, exportação (CSV/JSON).
- [ ] **Frontend** - interface para pedir dados, configurar/rodar backtests e visualizar resultados.
