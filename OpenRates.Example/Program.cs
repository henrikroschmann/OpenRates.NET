using OpenRates.Client;

var services = new ServiceCollection();

// Register OpenRates client with HttpClient factory and caching
services.AddHttpClient<IOpenRatesClient, OpenRatesClient>();
services.AddHybridCache();

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IOpenRatesClient>();

// Get current EUR to USD rate
var rate = await client.GetRateAsync("EUR", "USD");
Console.WriteLine($"EUR/USD: {rate}");

// Get historical rate
var historicalRate = await client.GetRateAsync(
    "EUR",
    "GBP",
    at: new DateTime(2025, 10, 30)
);
Console.WriteLine($"EUR/GBP on 2025-10-30: {historicalRate}");
