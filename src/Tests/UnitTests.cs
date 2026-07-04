using FluentAssertions;

namespace TwsApi.Tests;

/// <summary>Fast, gateway-free tests covering the pure/public surface. Always run.</summary>
public sealed class UnitTests
{
    [Fact]
    public void Contracts_Stock_sets_smart_usd_defaults()
    {
        var c = Contracts.Stock("AAPL");

        c.Symbol.Should().Be("AAPL");
        c.SecType.Should().Be("STK");
        c.Exchange.Should().Be("SMART");
        c.Currency.Should().Be("USD");
    }

    [Fact]
    public void Contracts_Option_populates_all_fields()
    {
        var c = Contracts.Option("AAPL", "20260116", 200, "C");

        c.SecType.Should().Be("OPT");
        c.Strike.Should().Be(200);
        c.Right.Should().Be("C");
        c.Multiplier.Should().Be("100");
    }

    [Theory]
    [InlineData(2104, true)]  // market data farm connected - informational
    [InlineData(2106, true)]  // hmds data farm connected - informational
    [InlineData(200, false)]  // no security definition - a real error
    [InlineData(201, false)]  // order rejected - a real error
    public void TwsException_IsInformational_classifies_codes(int code, bool expected)
    {
        TwsException.IsInformational(code).Should().Be(expected);
    }

    [Fact]
    public void TwsException_message_includes_code_and_reqId()
    {
        var ex = new TwsException(requestId: 42, errorCode: 200, message: "No security definition found");

        ex.RequestId.Should().Be(42);
        ex.ErrorCode.Should().Be(200);
        ex.Message.Should().Contain("200").And.Contain("42");
    }

    [Fact]
    public void ConnectionOptions_defaults_to_gateway_paper_port()
    {
        var options = new TwsConnectionOptions();

        options.Port.Should().Be(4002);
        options.Host.Should().Be("127.0.0.1");
    }
}
