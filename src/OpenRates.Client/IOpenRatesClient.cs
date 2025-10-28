namespace OpenRates.Client;

public interface IOpenRatesClient
{
    Task<decimal> GetRateAsync(string from, string to, DateTime? at = null, CancellationToken token = default);
}
