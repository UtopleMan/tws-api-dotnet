using System.Text.Json;
using FluentAssertions;
using RestApi.Orders;
using RestApi.PortfolioAnalyst;

namespace TwsApi.Tests;

/// <summary>Verifies the request enums serialize to the exact IBKR wire values. Always run.</summary>
public sealed class RestEnumTests
{
    [Theory]
    [InlineData(OrderSide.Buy, "\"BUY\"")]
    [InlineData(OrderSide.Sell, "\"SELL\"")]
    public void OrderSide_serializes_to_the_wire_value(OrderSide side, string expected) =>
        JsonSerializer.Serialize(side).Should().Be(expected);

    [Theory]
    [InlineData(PerformanceFrequency.Daily, "\"D\"")]
    [InlineData(PerformanceFrequency.Monthly, "\"M\"")]
    [InlineData(PerformanceFrequency.Quarterly, "\"Q\"")]
    public void PerformanceFrequency_serializes_to_the_wire_value(PerformanceFrequency freq, string expected) =>
        JsonSerializer.Serialize(freq).Should().Be(expected);
}
