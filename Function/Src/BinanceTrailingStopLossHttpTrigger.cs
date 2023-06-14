using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.Service;

namespace Function;

public static class BinanceTrailingStopLossHttpTrigger
{
    //[FunctionName("BinanceTrailingStopLossHttpTrigger")]
    public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("BinanceTrailingStopLossHttpTrigger function executed at: {Now}", DateTime.Now);
        try
        {
            var response = await TrailingStopLossService.UpdateStopLoss(log);
            log.LogInformation("BinanceTrailingStopLossHttpTrigger function finished at: {Now}", DateTime.Now);
            return new OkObjectResult(JsonConvert.SerializeObject(response));
        }
        catch (Exception e)
        {
            log.LogError(e, "Error in BinanceTrailingStopLossHttpTrigger.");
            return new BadRequestObjectResult(e);
        }
    }
}