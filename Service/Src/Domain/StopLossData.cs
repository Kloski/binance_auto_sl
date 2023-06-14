using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Microsoft.Extensions.Logging;

namespace Function.Domain;

public class StopLossData
{
    public BinanceExchangeInfo? ExchangeInfo { get; private set; }
    public Dictionary<string, BinanceBalance>? BalancesDict { get; private set; }
    public IEnumerable<BinanceOrder>? OpenOrders { get; private set; }
    public IEnumerable<string>? Symbols { get; private set; }
    public IEnumerable<BinancePrice>? Prices { get; private set; }

    public static async Task<StopLossData?> BuildStopLossData(BinanceClient binanceClient, string[] fiatCurrencies, ILogger logger)
    {
        var stopLossData = new StopLossData();

        var binanceClientSpotApi = binanceClient.SpotApi;
        var exchangeInfo = await binanceClientSpotApi.ExchangeData.GetExchangeInfoAsync();
        if (exchangeInfo.Success)
        {
            stopLossData.ExchangeInfo = exchangeInfo.Data;
        }
        else
        {
            logger.LogWarning("exchangeInfo is not Successful. exchangeInfo: {@ExchangeInfo}", exchangeInfo);
            return null;
        }

        var allPricesResponse = await binanceClient.SpotApi.ExchangeData.GetPricesAsync();
        if (allPricesResponse.Success)
        {
            stopLossData.Prices = allPricesResponse.Data;
        }
        else
        {
            logger.LogWarning("allPricesResponse is not Successful. allPricesResponse: {@AllPricesResponse}", allPricesResponse);
            return null;
        }

        var accInfoResponse = await binanceClientSpotApi.Account.GetAccountInfoAsync();
        if (accInfoResponse.Success)
        {
            var balances = accInfoResponse.Data.Balances.Where(b => b.Total > 0).ToList();
            var balancesDict = balances.ToDictionary(k => k.Asset, v => v);
            stopLossData.BalancesDict = balancesDict;

            var openOrdersResponse = await binanceClientSpotApi.Trading.GetOpenOrdersAsync();
            if (openOrdersResponse.Success)
            {
                stopLossData.OpenOrders = openOrdersResponse.Data;

                var balanceAssets = balances.Select(b => b.Asset).Where(a => !fiatCurrencies.Contains(a)).ToHashSet();
                var allAssets = balanceAssets.Select(a => string.Concat(a, fiatCurrencies[0])).ToHashSet();
                var allOrdersSymbols = openOrdersResponse.Data?.Select(oo => oo.Symbol).Distinct().ToHashSet();
                var allSymbols = allOrdersSymbols!.Union(allAssets);
                stopLossData.Symbols = allSymbols;
            }
            else
            {
                logger.LogWarning("openOrdersResponse is not Successful. openOrdersResponse: {@OpenOrdersResponse}", openOrdersResponse);
                return null;
            }
        }
        else
        {
            logger.LogWarning("accInfoResponse is not Successful. accInfoResponse: {@AccInfoResponse}", accInfoResponse);
            return null;
        }

        return stopLossData;
    }
}