using TwsApi.Rest.Account;
using TwsApi.Rest.Alerts;
using TwsApi.Rest.Ccp;
using TwsApi.Rest.Contract;
using TwsApi.Rest.Fyi;
using TwsApi.Rest.MarketData;
using TwsApi.Rest.Orders;
using TwsApi.Rest.Portfolio;
using TwsApi.Rest.PortfolioAnalyst;
using TwsApi.Rest.Scanner;
using TwsApi.Rest.Session;

namespace TwsApi.Rest;

/// <summary>
/// Typed client for the IBKR Client Portal Web API (the REST/HTTPS gateway). The full surface
/// is split into grouped sub-clients that mirror IBKR's own endpoint grouping; reach each group
/// through the properties below (e.g. <c>client.Portfolio.GetPositionsAsync(...)</c>).
///
/// The gateway holds the authenticated session established by the browser SSO login, so no
/// credentials are sent from here — construct with <see cref="RestClient(RestClientOptions)"/>
/// (or register via <c>AddIbkrRestClient</c>) and call. Depend on this interface rather than the
/// concrete <see cref="RestClient"/> so it can be stubbed in tests.
/// </summary>
public interface IRestClient : IDisposable
{
    /// <summary>Session lifecycle: auth status, tickle (keep-alive), reauthenticate, SSO validate, logout.</summary>
    ISessionApi Session { get; }

    /// <summary>Account metadata for the current session: accounts, switch, sub-accounts, P/L, trades.</summary>
    IAccountApi Account { get; }

    /// <summary>Contract search &amp; security definitions: search, secdef info/strikes, rules, algos, trsrv.</summary>
    IContractApi Contract { get; }

    /// <summary>Market data: live/regulatory snapshots and historical bars.</summary>
    IMarketDataApi MarketData { get; }

    /// <summary>Order lifecycle: live orders, place/preview (whatif), modify, cancel, reply to prompts.</summary>
    IOrdersApi Orders { get; }

    /// <summary>Portfolio: accounts, allocation, positions, per-position lookups, ledger, summary.</summary>
    IPortfolioApi Portfolio { get; }

    /// <summary>Portfolio Analyst: performance, transactions and summary across a date range.</summary>
    IPortfolioAnalystApi PortfolioAnalyst { get; }

    /// <summary>Market scanners: available parameters and running a scan.</summary>
    IScannerApi Scanner { get; }

    /// <summary>Price alerts (MTA and conditional): list, get, create/modify, activate, delete.</summary>
    IAlertsApi Alerts { get; }

    /// <summary>"For your information" notifications and their delivery/settings options.</summary>
    IFyiApi Fyi { get; }

    /// <summary>Consolidated Client Portal (CCP, beta): session, account, positions, orders, trades.</summary>
    ICcpApi Ccp { get; }
}
