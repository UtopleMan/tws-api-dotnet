using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestApi;

namespace TwsApi.Tests;

/// <summary>Gateway-free DI tests for <c>AddRestApi</c> and its named-client factory. Always run.</summary>
public sealed class RestApiDiTests
{
    [Fact]
    public void AddRestApi_default_registers_the_factory_and_configures_the_default_client()
    {
        using var provider = new ServiceCollection()
            .AddRestApi(o => o.BaseAddress = new Uri("https://default.example:5000"))
            .BuildServiceProvider();

        provider.GetRequiredService<IRestClientFactory>().Create().Should().NotBeNull();

        // The named HttpClient carries the configured base address (with the /v1/api suffix).
        var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(Options.DefaultName);
        http.BaseAddress.Should().Be(new Uri("https://default.example:5000/v1/api/"));
    }

    [Fact]
    public void AddRestApi_named_configures_one_client_per_name()
    {
        using var provider = new ServiceCollection()
            .AddRestApi("paper", o => o.BaseAddress = new Uri("https://paper.example:5000"))
            .AddRestApi("live", o => o.BaseAddress = new Uri("https://live.example:5001"))
            .BuildServiceProvider();

        var factory = provider.GetRequiredService<IRestClientFactory>();
        factory.Create("paper").Should().NotBeNull();
        factory.Create("live").Should().NotBeNull();

        var httpFactory = provider.GetRequiredService<IHttpClientFactory>();
        httpFactory.CreateClient("paper").BaseAddress.Should().Be(new Uri("https://paper.example:5000/v1/api/"));
        httpFactory.CreateClient("live").BaseAddress.Should().Be(new Uri("https://live.example:5001/v1/api/"));
    }

    [Fact]
    public void AddRestApi_registers_a_single_factory_across_multiple_named_calls()
    {
        var services = new ServiceCollection()
            .AddRestApi("a", _ => { })
            .AddRestApi("b", _ => { });

        services.Count(d => d.ServiceType == typeof(IRestClientFactory)).Should().Be(1);
    }

    [Fact]
    public void Factory_Create_with_explicit_options_overrides_the_configured_defaults()
    {
        using var provider = new ServiceCollection()
            .AddRestApi(o => o.BaseAddress = new Uri("https://default.example:5000"))
            .BuildServiceProvider();

        var factory = provider.GetRequiredService<IRestClientFactory>();

        using var client = factory.Create(new RestClientOptions { BaseAddress = new Uri("https://explicit.example:9000") });

        client.Should().NotBeNull();
    }
}
