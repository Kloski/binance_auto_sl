using System.Globalization;
using System.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using Function;
using Function.Domain;
using Microsoft.Extensions.Logging;

namespace Service.Service;

public static class TrailingStopLossService
{
    private static readonly string[] FiatCurrencies = { "USDT", "EUR" };
    private static readonly decimal StopLimitPerc = new(1.0002D);
    private static readonly decimal[] PercThresholds = { new(0.95D), new(0.91D), new(0.87D), new(0.83D), new(0.79D) };

    private static readonly double MinPrice = double.Parse(Environment.GetEnvironmentVariable("MinOrderPrice") ?? "50");


    public static async Task<(Dictionary<string, decimal> fiat, List<CurrencyState> crypto, string error, HttpStatusCode haResponseStatus)> UpdateStopLoss(ILogger log)
    {
        var errorMessage = "";
        var currencyStates = new List<CurrencyState>();
        var fiatCurrencies = new Dictionary<string, decimal>();
        try
        {
            var binanceClient = MyBinanceClient.getClient(log);
            var stopLossData = await StopLossData.BuildStopLossData(binanceClient, FiatCurrencies, log);
            currencyStates = await UpdateOrders(binanceClient, stopLossData, log);

            fiatCurrencies = stopLossData!.BalancesDict!
                .Where(i => FiatCurrencies.Contains(i.Key))
                .ToDictionary(i => i.Key, i => i.Value.Total);
        }
        catch (Exception e)
        {
            errorMessage = $"ReSetStopLossesOfOpenOrders failed. eMsg: {e.Message}";
            log.LogError(e, errorMessage);
        }

        var haResponseState = await HaService.SendStateToHa(new HaStatusMessage(fiatCurrencies, currencyStates, errorMessage));

        return (fiat: fiatCurrencies, crypto: currencyStates, error: errorMessage, haResponseStatus: haResponseState);
    }


    private static async Task<List<CurrencyState>> UpdateOrders(BinanceClient binanceClient, StopLossData? data, ILogger logger)
    {
        var fiatCurrency = FiatCurrencies[0];
        var pricesPerSymbol = data!.Prices?.Where(p => data.Symbols!.Contains(p.Symbol)).ToList();
        var symbolAssetDict = pricesPerSymbol!.Select(p => p.Symbol).ToDictionary(k => k, v => v.Replace(fiatCurrency, ""));

        List<CurrencyState> currencyStates = new();
        foreach (var openOrderCurrentPrice in pricesPerSymbol!)
        {
            var symbolStr = openOrderCurrentPrice.Symbol;
            try
            {
                var symbol = data.ExchangeInfo!.Symbols.FirstOrDefault(s => s.Name == symbolStr);
                var symbolOrders = data.OpenOrders!.Where(oo => oo.Symbol.Equals(symbolStr)).ToList();
                var cancellableSymbolOrders = symbolOrders.Where(o => !(o.IsWorking ?? false) && o.Status == OrderStatus.New)
                    .OrderByDescending(oo => oo.Price)
                    .ToList();

                var quantitySum = QuantitySum(data, symbolAssetDict, symbolStr, cancellableSymbolOrders);
                var ordersCount = OrdersCount(quantitySum, openOrderCurrentPrice);

                var logMessage = "";
                if (symbolOrders.Any())
                {
                    var currentPrice = cancellableSymbolOrders[0].Price;
                    var newPrice = openOrderCurrentPrice.Price * PercThresholds[0];
                    if (currentPrice < newPrice) // check if change is needed
                    {
                        foreach (var order in cancellableSymbolOrders) // cancel active orders
                        {
                            var cancelOrderAsync = await binanceClient.SpotApi.Trading.CancelOrderAsync(symbolStr, order.Id, order.ClientOrderId);
                        }

                        logMessage += $"Actual prices: {string.Join(", ", cancellableSymbolOrders.Select(so => so.Price).ToArray())}";
                        logMessage += await PlaceNewOrders(binanceClient, symbol!, quantitySum, ordersCount, openOrderCurrentPrice);
                    }
                }
                else // not existing orders
                {
                    logMessage += await PlaceNewOrders(binanceClient, symbol!, quantitySum, ordersCount, openOrderCurrentPrice);
                }

                logger.LogInformation($"Symbol: {symbolStr}; " + logMessage);
                currencyStates.Add(new CurrencyState(symbolStr, logMessage));
            }
            catch (Exception e)
            {
                var errorMessage = $"Re-setting orders for {symbolStr} failed.";
                logger.LogError(e, errorMessage);
                currencyStates.Add(new CurrencyState(symbolStr, errorMessage));
            }
        }

        return currencyStates;
    }

    private static async Task<string> PlaceNewOrders(BinanceClient binanceClient, BinanceSymbol symbol, decimal quantitySum, int ordersCount, BinancePrice openOrderCurrentPrice)
    {
        var pricePrecision = RoundDecimalToInt(symbol.PriceFilter!.TickSize);
        var amountPrecision = RoundDecimalToInt(symbol.LotSizeFilter!.StepSize);
        var quantityPerOrder = quantitySum / ordersCount;
        var newPrices = new decimal[ordersCount];
        var logMessage = "";
        for (var i = 0; i < ordersCount; i++)
        {
            newPrices[i] = openOrderCurrentPrice.Price * PercThresholds[i];
            var roundedPrice = decimal.Round(newPrices[i], pricePrecision, MidpointRounding.ToZero);
            var roundedStopPrice = decimal.Round(roundedPrice * StopLimitPerc, pricePrecision, MidpointRounding.ToZero);
            var roundedQuantity = decimal.Round(quantityPerOrder, amountPrecision, MidpointRounding.ToZero);
            var webCallResult = await binanceClient.SpotApi.Trading.PlaceOrderAsync(symbol.Name, OrderSide.Sell, SpotOrderType.StopLossLimit, roundedQuantity,
                price: roundedPrice, stopPrice: roundedStopPrice, timeInForce: TimeInForce.GoodTillCanceled);
            if (!webCallResult.Success) logMessage += $"; WebCallResult err: {webCallResult?.Error?.Message}, code: {webCallResult?.Error?.Code}";
        }

        return logMessage + $"; New prices: {string.Join(", ", newPrices)}";
    }

    private static int OrdersCount(decimal quantitySum, BinancePrice openOrderCurrentPrice)
    {
        var priceSum = quantitySum * openOrderCurrentPrice.Price;
        var ordersCount = PercThresholds.Length;
        var pricePerOrder = priceSum / ordersCount;
        if (decimal.ToDouble(pricePerOrder) < MinPrice) ordersCount = Math.Max(1, Convert.ToInt32(Math.Floor(decimal.ToDouble(priceSum) / MinPrice)));

        return ordersCount;
    }

    private static decimal QuantitySum(StopLossData? data, Dictionary<string, string> symbolAssetDict, string symbol, List<BinanceOrder> cancellableSymbolOrders)
    {
        var asset = symbolAssetDict[symbol];
        decimal quantitySum;
        if (data!.BalancesDict!.ContainsKey(asset))
        {
            var binanceBalance = data.BalancesDict[asset];
            quantitySum = binanceBalance.Total;
        }
        else
        {
            quantitySum = cancellableSymbolOrders.Select(o => o.Quantity).Sum();
        }

        return quantitySum;
    }

    private static int RoundDecimalToInt(decimal value)
    {
        return (decimal.GetBits(decimal.Parse(value.ToString(CultureInfo.InvariantCulture).TrimEnd('0')))[3] >> 16) & 0x000000FF;
    }
}

public readonly record struct CurrencyState(string Currency, string Message);