using Binance.Net.Clients;
using Binance.Net.Objects;
using Microsoft.Extensions.Logging;

namespace Function;

public class MyBinanceClient
{

    private static string GetBinanceSecret => Environment.GetEnvironmentVariable("Binance_Secret")!;
    private static string GetBinanceKey => Environment.GetEnvironmentVariable("Binance_Key")!;
    
    
    private static BinanceApiCredentials ApiCredentials;
    private static BinanceClient Client;

    public static BinanceClient getClient(ILogger logger)
    {
        logger.LogInformation($"Binance key: {GetBinanceKey}, Binance secret: {GetBinanceSecret}");
        if (Client == null)
        {
            ApiCredentials = new(GetBinanceKey, GetBinanceSecret);
            Client = new(new BinanceClientOptions { ApiCredentials = ApiCredentials });
        }
        return Client;
    }
}