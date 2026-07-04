using IBApi;

namespace TwsApi;

/// <summary>
/// Small factory helpers for the common <see cref="Contract"/> shapes, so callers
/// don't have to hand-populate the mutable POCO.
/// </summary>
public static class Contracts
{
    /// <summary>A stock (STK) contract, routed SMART by default.</summary>
    public static Contract Stock(string symbol, string exchange = "SMART", string currency = "USD") => new()
    {
        Symbol = symbol,
        SecType = "STK",
        Exchange = exchange,
        Currency = currency,
    };

    /// <summary>A cash/forex (CASH) contract, e.g. Forex("EUR", "USD") on IDEALPRO.</summary>
    public static Contract Forex(string baseCurrency, string quoteCurrency, string exchange = "IDEALPRO") => new()
    {
        Symbol = baseCurrency,
        SecType = "CASH",
        Exchange = exchange,
        Currency = quoteCurrency,
    };

    /// <summary>A futures (FUT) contract.</summary>
    public static Contract Future(string symbol, string lastTradeDateOrContractMonth, string exchange, string currency = "USD") => new()
    {
        Symbol = symbol,
        SecType = "FUT",
        LastTradeDateOrContractMonth = lastTradeDateOrContractMonth,
        Exchange = exchange,
        Currency = currency,
    };

    /// <summary>An option (OPT) contract.</summary>
    public static Contract Option(
        string symbol,
        string lastTradeDateOrContractMonth,
        double strike,
        string right,
        string exchange = "SMART",
        string currency = "USD",
        string multiplier = "100") => new()
    {
        Symbol = symbol,
        SecType = "OPT",
        LastTradeDateOrContractMonth = lastTradeDateOrContractMonth,
        Strike = strike,
        Right = right,
        Multiplier = multiplier,
        Exchange = exchange,
        Currency = currency,
    };
}
