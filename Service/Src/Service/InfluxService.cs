using Binance.Net.Objects.Models.Spot;
using Function;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Logging;

namespace Service.Service;

public static class InfluxService
{
    private static readonly string ExchangeName = "Binance";
    private static readonly string DB_URL = Environment.GetEnvironmentVariable("InfluxDB_URL")!;

    public static async Task StoreAccInfoToInfluxDb(ILogger log)
    {
        try
        {
            var database = "investment_balance";
            var retentionPolicy = "oneyear";

            // https://github.com/influxdata/influxdb-client-csharp#influxdb-18-api-compatibility
            // https://github.com/influxdata/influxdb-client-csharp/blob/master/Examples/InfluxDB18Example.cs
            using var influxBalanceClient = new InfluxDBClient(DB_URL, null, null, database, retentionPolicy);
            var todayRecordExists = await IsTodayRecordExists(database, retentionPolicy, influxBalanceClient);
            if (todayRecordExists)
            {
                log.LogInformation("Binance data already persisted for today. Skipping persisting to influx DB");
                return; // today data persisted
            }

            var binanceClient = MyBinanceClient.getClient(log);
            var allPricesResponse = await binanceClient.SpotApi.ExchangeData.GetPricesAsync();
            var accountInfoAsync = await binanceClient.SpotApi.Account.GetAccountInfoAsync();

            var (balanceDataPoint, assetDataPoints) = BuildInfluxPoints(accountInfoAsync.Data, allPricesResponse.Data);

            SaveInfluxPoints(influxBalanceClient, balanceDataPoint, assetDataPoints);
        }
        catch (Exception e)
        {
            var errorMessage = $"BinanceToInfluxTimerTrigger failed. eMsg: {e.Message}";
            log.LogError(e, errorMessage);
        }
    }

    private static void SaveInfluxPoints(InfluxDBClient influxBalanceClient, PointData balanceDataPoint, List<PointData> assetDataPoints)
    {
        using (var writeApi = influxBalanceClient.GetWriteApi())
        {
            writeApi.WritePoint(balanceDataPoint);
        }

        using var influxAssetClient = new InfluxDBClient(DB_URL, null, null, "investment_asset", "halfyear");
        using (var writeApi = influxAssetClient.GetWriteApi())
        {
            foreach (var assetDataPoint in assetDataPoints) writeApi.WritePoint(assetDataPoint);
        }
    }

    private static (PointData balanceDataPoint, List<PointData> assetDataPoints) BuildInfluxPoints(BinanceAccountInfo accountInfo, IEnumerable<BinancePrice> prices)
    {
        var sumFree = 0D;
        var sumTotal = 0D;
        var assetDataPoints = accountInfo.Balances.ToList()
            .Where(b => decimal.ToDouble(b.Total) > 0D)
            .Select(b =>
            {
                var rate = prices.FirstOrDefault(r => r.Symbol.Equals(b.Asset + "USDT"))?.Price ?? 1;
                var value = Convert.ToDouble(b.Total) * Convert.ToDouble(rate);

                sumFree += Convert.ToDouble(b.Available) * Convert.ToDouble(rate);
                sumTotal += value;

                return PointData.Measurement(ExchangeName)
                    .Tag("asset", b.Asset)
                    .Field("amount", b.Total) // amount of asset
                    .Field("rate", rate) // rate of asset in fiat (Asset_USDT)
                    .Field("fiat", value) // value of asset in fiat (USDT)
                    .Timestamp(DateTime.UtcNow, WritePrecision.S);
            })
            .ToList();

        PointData balanceDataPoint = PointData.Measurement(ExchangeName)
            //.Tag("???", ???)
            .Field("free", sumFree)
            .Field("total", sumTotal)
            .Timestamp(DateTime.UtcNow, WritePrecision.S);

        return (balanceDataPoint, assetDataPoints);
    }

    private static async Task<bool> IsTodayRecordExists(string database, string retentionPolicy, InfluxDBClient influxBalanceClient)
    {
        var query = $"from(bucket: \"{database}/{retentionPolicy}\") |> range(start: -24h)";
        var fluxTables = await influxBalanceClient.GetQueryApi().QueryAsync(query);
        if (fluxTables.Count <= 0) return false;
        var fluxRecords = fluxTables[0].Records;
        return fluxRecords.FirstOrDefault(r => r.GetTime() != null && r.GetTime()!.Value.ToDateTimeUtc() > DateTime.Today) != null;
    }
}