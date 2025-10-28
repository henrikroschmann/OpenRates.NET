using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using OpenRates.Core.Models;

namespace OpenRates.Core.Providers;

/// <summary>
/// Provides functionality to retrieve and parse daily foreign exchange rates from the European Central Bank (ECB).
/// </summary>
/// <remarks>This class is sealed and cannot be inherited. It fetches exchange rates with respect to the Euro
/// (EUR) and constructs a mapping for two-way currency conversion. Instances of this class are intended for use in
/// scenarios where up-to-date ECB exchange rates are required, such as financial applications or currency
/// converters.</remarks>
public sealed class EcbProvider(HttpClient http, ILogger<EcbProvider>? logger = null)
{
    private const string EcbDailyRatesUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
    private readonly ILogger<EcbProvider>? _logger = logger;

    public async Task<ExchangeRates> FetchAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Fetching ECB daily exchange rates from {Url}", EcbDailyRatesUrl);

            var xml = await _http.GetStringAsync(EcbDailyRatesUrl, cancellationToken);
            
            var doc = XDocument.Parse(xml);
            var ns = doc.Root?.GetDefaultNamespace() ?? throw new InvalidOperationException("ECB XML response has no root element");
            
            var cubes = doc.Descendants(ns + "Cube")
                           .Where(x => x.Attribute("currency") != null)
                           .ToList();

            if (cubes.Count == 0)
            {
                _logger?.LogWarning("No currency rates found in ECB response");
                return new ExchangeRates { Rates = new Dictionary<string, Dictionary<string, decimal>>() };
            }

            var rates = cubes.ToDictionary(
                x => x.Attribute("currency")!.Value.ToLowerInvariant(),
                x => decimal.Parse(x.Attribute("rate")!.Value, CultureInfo.InvariantCulture));

            // ECB gives everything vs EUR; invert to build two-way map
            var map = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["eur"] = rates
            };
            
            foreach (var kvp in rates)
            {
                map[kvp.Key] = new Dictionary<string, decimal> { ["eur"] = 1 / kvp.Value };
            }

            _logger?.LogInformation("Successfully fetched {Count} ECB exchange rates", rates.Count);
            return new ExchangeRates { Rates = map };
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "Failed to fetch ECB exchange rates from {Url}", EcbDailyRatesUrl);
            throw new InvalidOperationException("Failed to retrieve ECB exchange rates", ex);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger?.LogError(ex, "Failed to parse ECB exchange rates");
            throw new InvalidOperationException("Failed to parse ECB exchange rates", ex);
        }
    }
}
