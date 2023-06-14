using System.Threading.Tasks;
using ConsoleFunctionTrigger;
using NUnit.Framework;
using Service.Service;

namespace MyFunctionsTest;

public class BinanceTrailingStopLossTimerTriggerTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task StopLossTest()
    {
        var logger = Helper.CreateConsoleLogger<BinanceTrailingStopLossTimerTriggerTests>();
        Helper.InitContext(logger);

        var (fiat, crypto, error, haResponseStatus) = await TrailingStopLossService.UpdateStopLoss(logger);

        // var binanceClient = new BinanceClient(new BinanceClientOptions { ApiCredentials = BinanceTrailingStopLossTimerTrigger.ApiCredentials });
        // var stopLossData = await StopLossData.BuildStopLossData(binanceClient, BinanceTrailingStopLossTimerTrigger.FiatCurrencies, logger);
        // await BinanceTrailingStopLossTimerTrigger.UpdateOrders(binanceClient, stopLossData, logger);

        Assert.Pass();
    }

    [Test]
    public async Task StoreAccInfoToInfluxDbTest()
    {
        var logger = Helper.CreateConsoleLogger<BinanceTrailingStopLossTimerTriggerTests>();
        Helper.InitContext(logger);

        await InfluxService.StoreAccInfoToInfluxDb(logger);

        // var binanceClient = new BinanceClient(new BinanceClientOptions { ApiCredentials = BinanceTrailingStopLossTimerTrigger.ApiCredentials });
        // var stopLossData = await StopLossData.BuildStopLossData(binanceClient, BinanceTrailingStopLossTimerTrigger.FiatCurrencies, logger);
        // await BinanceTrailingStopLossTimerTrigger.UpdateOrders(binanceClient, stopLossData, logger);

        Assert.Pass();
    }
}