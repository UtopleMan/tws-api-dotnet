using System.Text.Json.Serialization;
using RestApi.Internal;

namespace RestApi.Scanner;

// ---------------------------------------------------------------------------
// GET /iserver/scanner/params
// ---------------------------------------------------------------------------

/// <summary>
/// The scanner parameter catalogue (<c>GET /iserver/scanner/params</c>): the four lists needed to
/// assemble a <see cref="ScannerRequest"/>.
/// </summary>
public sealed record ScannerParamsResponse
{
    /// <summary>Available scan types (e.g. <c>TOP_PERC_GAIN</c>) and the instruments they apply to.</summary>
    [JsonPropertyName("scan_type_list")]
    public IReadOnlyList<ScannerScanType>? ScanTypeList { get; init; }

    /// <summary>Available instruments and the filter codes valid for each.</summary>
    [JsonPropertyName("instrument_list")]
    public IReadOnlyList<ScannerInstrument>? InstrumentList { get; init; }

    /// <summary>Available filters that can be applied to a scanner request.</summary>
    [JsonPropertyName("filter_list")]
    public IReadOnlyList<ScannerFilterDefinition>? FilterList { get; init; }

    /// <summary>Tree of available scan locations (markets/regions).</summary>
    [JsonPropertyName("location_tree")]
    public IReadOnlyList<ScannerLocationTree>? LocationTree { get; init; }
}

/// <summary>A scan type entry in <see cref="ScannerParamsResponse.ScanTypeList"/>.</summary>
public sealed record ScannerScanType
{
    /// <summary>Human-readable scan type name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    /// <summary>Scan type code passed as <see cref="ScannerRequest.Type"/>.</summary>
    public string? Code { get; init; }

    /// <summary>Instrument types this scan applies to.</summary>
    public IReadOnlyList<string>? Instruments { get; init; }
}

/// <summary>An instrument entry in <see cref="ScannerParamsResponse.InstrumentList"/>.</summary>
public sealed record ScannerInstrument
{
    /// <summary>Human-readable instrument name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    /// <summary>Instrument type passed as <see cref="ScannerRequest.Instrument"/>.</summary>
    public string? Type { get; init; }

    /// <summary>Codes of the filters valid for this instrument.</summary>
    public IReadOnlyList<string>? Filters { get; init; }
}

/// <summary>A filter definition in <see cref="ScannerParamsResponse.FilterList"/>.</summary>
public sealed record ScannerFilterDefinition
{
    /// <summary>Group the filter belongs to.</summary>
    public string? Group { get; init; }

    /// <summary>Human-readable filter name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    /// <summary>Filter code used in a <see cref="ScannerFilter"/>.</summary>
    public string? Code { get; init; }

    /// <summary>Value type expected by the filter.</summary>
    public string? Type { get; init; }
}

/// <summary>A location tree node in <see cref="ScannerParamsResponse.LocationTree"/>.</summary>
public sealed record ScannerLocationTree
{
    /// <summary>Human-readable location name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    /// <summary>Location type.</summary>
    public string? Type { get; init; }

    /// <summary>Child locations under this node.</summary>
    public IReadOnlyList<ScannerLocation>? Locations { get; init; }
}

/// <summary>A location leaf in <see cref="ScannerLocationTree.Locations"/>.</summary>
public sealed record ScannerLocation
{
    /// <summary>Human-readable location name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    /// <summary>Location type/code passed as <see cref="ScannerRequest.Location"/>.</summary>
    public string? Type { get; init; }
}

// ---------------------------------------------------------------------------
// POST /iserver/scanner/run
// ---------------------------------------------------------------------------

/// <summary>Request body for the brokerage scanner (<c>POST /iserver/scanner/run</c>).</summary>
public sealed record ScannerRequest
{
    /// <summary>Instrument type, e.g. <c>STK</c>.</summary>
    public string? Instrument { get; init; }

    /// <summary>Scan type, e.g. <c>TOP_PERC_GAIN</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Filters to apply to the scan.</summary>
    public IReadOnlyList<ScannerFilter>? Filter { get; init; }

    /// <summary>Location to scan.</summary>
    public string? Location { get; init; }

    /// <summary>Number of rows to return.</summary>
    public int? Size { get; init; }
}

/// <summary>A single filter in a <see cref="ScannerRequest"/>.</summary>
public sealed record ScannerFilter
{
    /// <summary>Filter code (from <see cref="ScannerFilterDefinition.Code"/>).</summary>
    public string? Code { get; init; }

    /// <summary>Filter value.</summary>
    public double? Value { get; init; }
}

/// <summary>A contract returned by the brokerage scanner (<c>POST /iserver/scanner/run</c>).</summary>
public sealed record ScannerContract
{
    /// <summary>Server-assigned request id.</summary>
    [JsonPropertyName("server_id")]
    public string? ServerId { get; init; }

    /// <summary>Column name.</summary>
    [JsonPropertyName("column_name")]
    public string? ColumnName { get; init; }

    /// <summary>Underlying symbol.</summary>
    public string? Symbol { get; init; }

    /// <summary>Conid and exchange. Format supports <c>conid</c> or <c>conid@exchange</c>.</summary>
    public string? Conidex { get; init; }

    /// <summary>Contract identifier.</summary>
    [JsonPropertyName("con_id")]
    public long? ConId { get; init; }

    /// <summary>List of available chart periods.</summary>
    [JsonPropertyName("available_chart_periods")]
    public string? AvailableChartPeriods { get; init; }

    /// <summary>Contract's company name.</summary>
    [JsonPropertyName("company_name")]
    public string? CompanyName { get; init; }

    /// <summary>Formatted contract name, e.g. <c>FB Stock (NASDAQ.NMS)</c>.</summary>
    [JsonPropertyName("contract_description_1")]
    public string? ContractDescription1 { get; init; }

    /// <summary>Listing exchange.</summary>
    [JsonPropertyName("listing_exchange")]
    public string? ListingExchange { get; init; }

    /// <summary>Security type.</summary>
    [JsonPropertyName("sec_type")]
    public string? SecType { get; init; }
}

// ---------------------------------------------------------------------------
// POST /hmds/scanner
// ---------------------------------------------------------------------------

/// <summary>Request body for the HMDS market-data scanner (<c>POST /hmds/scanner</c>).</summary>
public sealed record HmdsScannerRequest
{
    /// <summary>Instrument type, e.g. <c>BOND.GOVT</c>.</summary>
    public string? Instrument { get; init; }

    /// <summary>Locations to scan, e.g. <c>BOND.GOVT.US</c>.</summary>
    public string? Locations { get; init; }

    /// <summary>Scan code, e.g. <c>FAR_MATURITY_DATE</c>.</summary>
    public string? ScanCode { get; init; }

    /// <summary>Security type, e.g. <c>BOND</c>.</summary>
    public string? SecType { get; init; }

    /// <summary>Filters to apply to the scan.</summary>
    public IReadOnlyList<HmdsScannerFilter>? Filters { get; init; }
}

/// <summary>A single filter in a <see cref="HmdsScannerRequest"/>.</summary>
public sealed record HmdsScannerFilter
{
    /// <summary>Filter code, e.g. <c>bondValidNetBidOrAskOnly</c>.</summary>
    public string? Code { get; init; }

    /// <summary>
    /// Filter value — an integer, double, boolean or string depending on the filter specified in
    /// <see cref="Code"/>.
    /// </summary>
    public System.Text.Json.Nodes.JsonNode? Value { get; init; }
}

/// <summary>Result of the HMDS scanner (<c>POST /hmds/scanner</c>).</summary>
public sealed record ScannerResult
{
    /// <summary>Total number of matches.</summary>
    public int? Total { get; init; }

    /// <summary>Number of rows returned.</summary>
    public int? Size { get; init; }

    /// <summary>Offset into the result set.</summary>
    public int? Offset { get; init; }

    /// <summary>Time the scan was run.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? ScanTime { get; init; }

    /// <summary>Scan id (e.g. <c>scanner2</c>).</summary>
    public string? Id { get; init; }

    /// <summary>Position marker.</summary>
    public string? Position { get; init; }

    /// <summary>Contracts matching the scanner query.</summary>
    public ScannerResultContracts? Contracts { get; init; }
}

/// <summary>Wrapper holding the list of contracts in a <see cref="ScannerResult"/>.</summary>
public sealed record ScannerResultContracts
{
    /// <summary>List of contracts matching the scanner query.</summary>
    public IReadOnlyList<ScannerResultContract>? Contract { get; init; }
}

/// <summary>A contract in a <see cref="ScannerResult"/>.</summary>
public sealed record ScannerResultContract
{
    /// <summary>Time the contract entered the scan.</summary>
    [JsonConverter(typeof(IbkrTimestampConverter))]
    public DateTimeOffset? InScanTime { get; init; }

    /// <summary>Distance/rank of the contract in the scan.</summary>
    public int? Distance { get; init; }

    /// <summary>Contract identifier.</summary>
    public long? ContractID { get; init; }
}
