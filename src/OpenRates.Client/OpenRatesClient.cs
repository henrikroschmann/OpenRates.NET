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

                    if (fromLower == toLower)
                    {
                        return 1m;
                    }

                    if (!json.Rates.TryGetValue(fromLower, out var fromRates) ||
                        !fromRates.TryGetValue(toLower, out var direct))
                    {
                        const string eur = "eur";

                        if (!json.Rates.TryGetValue(eur, out var eurRates))
                        {
                            throw new KeyNotFoundException($"Base {eur} rates not available for {from}/{to}");
                        }

                        // EUR -> TO
                        if (fromLower == eur && eurRates.TryGetValue(toLower, out var eurToTo))
                        {
                            return eurToTo;
                        }

                        // FROM -> EUR
                        if (toLower == eur && eurRates.TryGetValue(fromLower, out var eurToFrom))
                        {
                            return eurToFrom == 0m
                                ? throw new DivideByZeroException($"Zero rate for {eur}->{fromLower}")
                                : 1m / eurToFrom;
                        }

                        // FROM -> TO via EUR
                        return eurRates.TryGetValue(toLower, out var eurToTo2) &&
                            eurRates.TryGetValue(fromLower, out var eurToFrom2)
                            ? eurToFrom2 == 0m
                                ? throw new DivideByZeroException($"Zero rate for {eur}->{fromLower}")
                                : eurToTo2 / eurToFrom2
                            : throw new KeyNotFoundException($"Exchange rate not found for {from}/{to}");
                    }

                    return direct;
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
