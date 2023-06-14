using Service.Service;

namespace Function.Domain;

public interface IHaMessage
{
    public string ToString();
}

public readonly struct HaStatusMessage : IHaMessage
{
    public HaStatusMessage(Dictionary<string, decimal> fiatCurrencies, List<CurrencyState> ordersStates, string errorMessage)
    {
        FiatCurrencies = fiatCurrencies;
        OrdersStates = ordersStates;
        ErrorMessage = errorMessage;
    }

    private Dictionary<string, decimal> FiatCurrencies { get; }
    private List<CurrencyState> OrdersStates { get; }
    private string ErrorMessage { get; }

    public override string ToString()
    {
        return $"{FiatCurrencies.Select(c => $"{c.Key}={c.Value}").Aggregate("", (i, j) => $"{i},{j}")} | " +
               $"{OrdersStates.Where(s => !string.IsNullOrWhiteSpace(s.Message)).Select(s => s.Currency).Aggregate("", (i, j) => $"{i}, {j}")} | " +
               $"{ErrorMessage}";
    }
}