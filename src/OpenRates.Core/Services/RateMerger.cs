using OpenRates.Core.Models;

namespace OpenRates.Core.Services;

public static class RateMerger
{
    public static ExchangeRates Merge(params ExchangeRates[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources, nameof(sources));

        if (sources.Length == 0)
        {
            return new ExchangeRates { Date = DateTime.UtcNow.Date };
        }

        var merged = new ExchangeRates { Date = DateTime.UtcNow.Date };
        
        foreach (var src in sources)
        {
            if (src?.Rates == null)
            {
                continue;
            }

            foreach (var baseCcy in src.Rates)
            {
                if (string.IsNullOrWhiteSpace(baseCcy.Key) || baseCcy.Value == null)
                {
                    continue;
                }

                if (!merged.Rates.TryGetValue(baseCcy.Key, out var inner))
                {
                    merged.Rates[baseCcy.Key] = inner = [];
                }

                foreach (var kvp in baseCcy.Value)
                {
                    if (!string.IsNullOrWhiteSpace(kvp.Key))
                    {
                        inner[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        
        return merged;
    }
}