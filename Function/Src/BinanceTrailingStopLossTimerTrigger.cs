using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.Service;

namespace Function;

public static class BinanceTrailingStopLossTimerTrigger
{
    [FunctionName("BinanceTrailingStopLossTimerTrigger")]
    public static async Task<IActionResult> Run([TimerTrigger("0 45 */6 * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation("BinanceTrailingStopLossTimerTrigger function executed at: {Now}", DateTime.Now);
        try
        {
            var response = await TrailingStopLossService.UpdateStopLoss(log);
            log.LogInformation("BinanceTrailingStopLossTimerTrigger function finished at: {Now}", DateTime.Now);
            return new OkObjectResult(JsonConvert.SerializeObject(response));
        }
        catch (Exception e)
        {
            log.LogError(e, "Error in BinanceTrailingStopLossHttpTrigger.");
            return new BadRequestObjectResult(e);
        }

        log.LogInformation("BinanceTrailingStopLossTimerTrigger function finished at: {Now}", DateTime.Now);
    }
}