using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Hybrid;

namespace OpenRates.Client;

public sealed class OpenRatesClient(HttpClient http, HybridCache cache) : IOpenRatesClient
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
    private readonly HybridCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task<decimal> GetRateAsync(string from, string to, DateTime? at = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(from, nameof(from));
        ArgumentException.ThrowIfNullOrWhiteSpace(to, nameof(to));

        var fromLower = from.ToLowerInvariant();
        var toLower = to.ToLowerInvariant();
        var effectiveDate = at?.Date ?? DateTime.UtcNow.Date;
        var key = $"{fromLower}:{toLower}:{effectiveDate:yyyy-MM-dd}";

        return await _cache.GetOrCreateAsync(
            key,
            async cancel =>
            {
                var dateSegment = at?.ToString("yyyy-MM-dd") ?? "latest";
                var url = $"https://cdn.jsdelivr.net/gh/henrikroschmann/OpenRates.NET@main/data/{dateSegment}.json";

                try
                {
                    var json = await _http.GetFromJsonAsync<ExchangeRatesResponse>(url, cancel)
                        ?? throw new InvalidOperationException($"Invalid response from CDN for {fromLower}/{toLower}");

                    if (!json.Rates.TryGetValue(fromLower, out var fromRates))
                    {
                        throw new KeyNotFoundException($"Exchange rate not found for {from}/{to}");
                    }
       

                    if (!fromRates.TryGetValue(toLower, out var result))
                    {
                        throw new KeyNotFoundException($"Exchange rate not found for {from}/{to}");
                    }

                    return result;
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException($"Failed to fetch exchange rate for {from}/{to} at {dateSegment}", ex);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Failed to parse exchange rate response for {from}/{to}", ex);
                }
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(12),
                LocalCacheExpiration = TimeSpan.FromHours(12)
            },
            cancellationToken: token
        );
    }
}

public sealed record ExchangeRatesResponse(
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("rates")] IDictionary<string, IDictionary<string, decimal>> Rates
);
