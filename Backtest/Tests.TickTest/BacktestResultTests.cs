using Domain.TickTest.Models;
using Domain.TickTest.Enums;
using static Tests.TickTest.Support.ContextBuilder;

namespace Tests.TickTest
{
    public class BacktestResultTests
    {
        // One long trade of 1 contract from 100 to 100 + pnl.
        private static ClosedPosition Trade(double pnl) => new()
        {
            Asset = TestAsset, Side = EPositionSide.Long, Quantity = 1, EntryPrice = 100, ExitPrice = 100 + pnl
        };

        private static BacktestResult Result(double costs, params double[] pnls) => new()
        {
            ClosedPositions = pnls.Select(Trade).ToList(),
            Costs = costs
        };

        [Fact]
        public void Statistics_DefaultResult_AreAllZero()
        {
            var result = default(BacktestResult);

            Assert.Equal(0, result.GrossPnL);
            Assert.Equal(0, result.RealizedPnL);
            Assert.Equal(0, result.NumberOfTrades);
            Assert.Equal(0, result.WinRate);
            Assert.Equal(0, result.ProfitFactor);
            Assert.Equal(0, result.PayOff);
        }

        [Fact]
        public void Statistics_NoTrades_AreAllZero()
        {
            var result = Result(costs: 0);

            Assert.Equal(0, result.NumberOfTrades);
            Assert.Equal(0, result.WinRate);
            Assert.Equal(0, result.ProfitFactor);
            Assert.Equal(0, result.PayOff);
        }

        [Fact]
        public void GrossPnL_SeveralTrades_SumsTheirPnL()
        {
            Assert.Equal(15, Result(costs: 3, 30, -10, -5).GrossPnL);
        }

        [Fact]
        public void RealizedPnL_WithCosts_IsGrossMinusCosts()
        {
            Assert.Equal(12, Result(costs: 3, 30, -10, -5).RealizedPnL);
        }

        [Fact]
        public void RealizedPnL_NoCosts_EqualsGross()
        {
            Assert.Equal(15, Result(costs: 0, 30, -10, -5).RealizedPnL);
        }

        [Fact]
        public void RealizedPnL_CostsWithoutTrades_IsNegativeCosts()
        {
            // An open position already paid for its entry fill.
            Assert.Equal(-2, Result(costs: 2).RealizedPnL);
        }

        [Fact]
        public void NumberOfTrades_SeveralTrades_CountsClosedPositions()
        {
            Assert.Equal(3, Result(costs: 0, 30, -10, -5).NumberOfTrades);
        }

        [Fact]
        public void WinRate_OneWinnerInFour_Is25Percent()
        {
            Assert.Equal(25, Result(costs: 0, 30, -10, -5, -1).WinRate);
        }

        [Fact]
        public void WinRate_BreakevenTrade_IsNotAWin()
        {
            Assert.Equal(50, Result(costs: 0, 30, 0).WinRate);
        }

        [Fact]
        public void WinRate_Costs_DoNotChangeIt()
        {
            Assert.Equal(100, Result(costs: 100, 30).WinRate);
        }

        [Fact]
        public void ProfitFactor_WinsAndLosses_IsGrossProfitOverGrossLoss()
        {
            Assert.Equal(2, Result(costs: 0, 20, 10, -10, -5).ProfitFactor);
        }

        [Fact]
        public void ProfitFactor_NoLosingTrade_IsZero()
        {
            Assert.Equal(0, Result(costs: 0, 20, 10).ProfitFactor);
        }

        [Fact]
        public void ProfitFactor_OnlyLosingTrades_IsZero()
        {
            Assert.Equal(0, Result(costs: 0, -20, -10).ProfitFactor);
        }

        [Fact]
        public void PayOff_WithCosts_IsNetPnLPerTrade()
        {
            Assert.Equal(4, Result(costs: 3, 30, -10, -5).PayOff);
        }

        [Fact]
        public void Statistics_ShortTrades_UseThePositionPnL()
        {
            var result = new BacktestResult
            {
                ClosedPositions = new List<ClosedPosition>
                {
                    new() { Asset = TestAsset, Side = EPositionSide.Short, Quantity = 2, EntryPrice = 110, ExitPrice = 100 },
                    new() { Asset = TestAsset, Side = EPositionSide.Short, Quantity = 1, EntryPrice = 100, ExitPrice = 105 }
                }
            };

            Assert.Equal(15, result.GrossPnL);
            Assert.Equal(50, result.WinRate);
            Assert.Equal(4, result.ProfitFactor);
        }
    }
}
