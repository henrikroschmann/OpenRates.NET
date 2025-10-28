namespace OpenRates.Core.Models;

public sealed class ExchangeRates
{
    public DateTime Date { get; init; } = DateTime.UtcNow.Date;
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; init; } = [];

    public decimal? TryGet(string from, string to)
    {
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return null;
        }

        var fromLower = from.ToLowerInvariant();
        var toLower = to.ToLowerInvariant();

        return Rates.TryGetValue(fromLower, out var map) &&
            map.TryGetValue(toLower, out var rate)
            ? rate
            : null;
    }
}