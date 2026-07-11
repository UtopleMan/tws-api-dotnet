using System.Net;
using System.Text;
using FluentAssertions;
using RestApi;

namespace TwsApi.Tests;

/// <summary>
/// Gateway-free tests for <see cref="RestApi.Contract.ContractApi"/> deserialization edge cases,
/// driven through the public <see cref="RestClient(HttpClient)"/> constructor with a stubbed handler.
/// </summary>
public sealed class ContractApiTests
{
    [Fact]
    public async Task SearchSecDefAsync_returns_empty_for_object_error_envelope()
    {
        // For a symbol it cannot resolve, the gateway replies with an object-shaped envelope
        // ({ "error": ... }) instead of an empty array. The reader must tolerate that, not throw.
        using var client = ClientReturning("""{"error":"No security definition found"}""");

        var results = await client.Contract.SearchSecDefAsync("ZSOL");

        results.Should().NotBeNull();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchSecDefAsync_returns_empty_for_empty_body()
    {
        using var client = ClientReturning("");

        var results = await client.Contract.SearchSecDefAsync("ZSOL");

        results.Should().NotBeNull();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchSecDefAsync_parses_array_results()
    {
        using var client = ClientReturning("""[{"conid":265598,"symbol":"AAPL","companyName":"APPLE INC"}]""");

        var results = await client.Contract.SearchSecDefAsync("AAPL");

        results.Should().ContainSingle();
        results![0].Conid.Should().Be(265598);
        results[0].Symbol.Should().Be("AAPL");
    }

    private static RestClient ClientReturning(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var http = new HttpClient(new StubHandler(body, status))
        {
            BaseAddress = new Uri("https://stub.test/v1/api/"),
        };
        return new RestClient(http);
    }

    private sealed class StubHandler(string body, HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
