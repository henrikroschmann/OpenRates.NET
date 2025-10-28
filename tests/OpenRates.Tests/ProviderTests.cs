using Microsoft.Extensions.Logging.Abstractions;
using OpenRates.Core.Models;
using OpenRates.Core.Providers;
using OpenRates.Core.Services;

namespace OpenRates.Tests;

public class ProviderTests
{
    [Fact(DisplayName = "ECB provider fetches rates successfully")]
    public async Task FetchAsync_WithValidEcbEndpoint_ReturnsExchangeRates()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var logger = NullLogger<EcbProvider>.Instance;
        var provider = new EcbProvider(httpClient, logger);

        // Act
        var rates = await provider.FetchAsync();

        // Assert
        Assert.NotNull(rates);
        Assert.NotEmpty(rates.Rates);
        Assert.Contains("eur", rates.Rates.Keys);
        Assert.Contains("usd", rates.Rates["eur"].Keys);
        Assert.True(rates.Rates["eur"]["usd"] > 0);
    }

    [Fact(DisplayName = "ECB provider includes inverse rates")]
    public async Task FetchAsync_WhenSuccessful_IncludesInverseRates()
    {
        // Arrange
        using var httpClient = new HttpClient();
        var provider = new EcbProvider(httpClient);

        // Act
        var rates = await provider.FetchAsync();

        // Assert
        Assert.Contains("usd", rates.Rates.Keys);
        Assert.Contains("eur", rates.Rates["usd"].Keys);
        
        var eurToUsd = rates.Rates["eur"]["usd"];
        var usdToEur = rates.Rates["usd"]["eur"];
        Assert.True(Math.Abs(eurToUsd * usdToEur - 1) < 0.0001m);
    }

    [Fact(DisplayName = "Rate merger handles empty sources")]
    public void Merge_WithEmptySources_ReturnsEmptyRates()
    {
        // Act
        var result = RateMerger.Merge();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Rates);
    }

    [Fact(DisplayName = "Rate merger combines multiple sources")]
    public void Merge_WithMultipleSources_CombinesAllRates()
    {
        // Arrange
        var source1 = new ExchangeRates
        {
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["eur"] = new() { ["usd"] = 1.1m }
            }
        };

        var source2 = new ExchangeRates
        {
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["gbp"] = new() { ["usd"] = 1.3m }
            }
        };

        // Act
        var merged = RateMerger.Merge(source1, source2);

        // Assert
        Assert.Contains("eur", merged.Rates.Keys);
        Assert.Contains("gbp", merged.Rates.Keys);
        Assert.Equal(1.1m, merged.Rates["eur"]["usd"]);
        Assert.Equal(1.3m, merged.Rates["gbp"]["usd"]);
    }

    [Fact(DisplayName = "Rate merger handles null sources gracefully")]
    public void Merge_WithNullSource_SkipsNullEntries()
    {
        // Arrange
        var validSource = new ExchangeRates
        {
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["eur"] = new() { ["usd"] = 1.1m }
            }
        };

        // Act
        var merged = RateMerger.Merge(validSource, null!);

        // Assert
        Assert.NotNull(merged);
        Assert.Contains("eur", merged.Rates.Keys);
    }

    [Fact(DisplayName = "ExchangeRates TryGet returns rate when found")]
    public void TryGet_WithValidCurrencies_ReturnsRate()
    {
        // Arrange
        var rates = new ExchangeRates
        {
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["eur"] = new() { ["usd"] = 1.1m }
            }
        };

        // Act
        var rate = rates.TryGet("EUR", "USD");

        // Assert
        Assert.NotNull(rate);
        Assert.Equal(1.1m, rate.Value);
    }

    [Fact(DisplayName = "ExchangeRates TryGet returns null when not found")]
    public void TryGet_WithInvalidCurrencies_ReturnsNull()
    {
        // Arrange
        var rates = new ExchangeRates
        {
            Rates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["eur"] = new() { ["usd"] = 1.1m }
            }
        };

        // Act
        var rate = rates.TryGet("JPY", "CNY");

        // Assert
        Assert.Null(rate);
    }

    [Fact(DisplayName = "ExchangeRates TryGet handles null input")]
    public void TryGet_WithNullInput_ReturnsNull()
    {
        // Arrange
        var rates = new ExchangeRates();

        // Act
        var rate = rates.TryGet(null!, null!);

        // Assert
        Assert.Null(rate);
    }
}
