using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Service.Service;

namespace Function;

public static class BinanceToInfluxTimerTrigger
{
    [FunctionName("BinanceToInfluxTimerTrigger")]
    public static async Task Run([TimerTrigger("0 45 6 * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation("BinanceToInfluxTimerTrigger function executed at: {Now}", DateTime.Now);

        await InfluxService.StoreAccInfoToInfluxDb(log);

        // currently, we do not need this:
        // var haResponseState = await HaService.SendStateToHa(new HaStatusMessage(fiatCurrencies, currencyStates, errorMessage));

        log.LogInformation("BinanceToInfluxTimerTrigger function finished at: {Now}", DateTime.Now);
    }
}