using ConsoleFunctionTrigger;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.Service;

var log = Helper.CreateConsoleLogger<Program>();
Helper.InitContext(log);

try
{
    log.LogInformation("TrailingStopLossService.UpdateStopLoss execution started at: {Now}", DateTime.Now);
    var response = await TrailingStopLossService.UpdateStopLoss(log);
    log.LogInformation("TrailingStopLossService.UpdateStopLoss execution finished at: {Now}. Response: {Response}", DateTime.Now, JsonConvert.SerializeObject(response));
}
catch (Exception e)
{
    log.LogError(e, "Error in TrailingStopLossService.UpdateStopLoss.");
}

log.LogInformation("BinanceToInfluxTimerTrigger function executed at: {Now}", DateTime.Now);
await InfluxService.StoreAccInfoToInfluxDb(log);
log.LogInformation("BinanceToInfluxTimerTrigger function finished at: {Now}", DateTime.Now);
// currently, we do not need this:
// var haResponseState = await HaService.SendStateToHa(new HaStatusMessage(fiatCurrencies, currencyStates, errorMessage));